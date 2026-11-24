using Attendance.Data;
using Attendance.Domain;
using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Attendance.Services;

public sealed class AttendanceService(
    IDbContextFactory<AttendanceDbContext> dbFactory,
    UserSession session,
    IOptions<AttendanceOptions> options) : IAttendanceService
{
    public async Task<IReadOnlyList<Lesson>> GetLessonsAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await AccessScope.Lessons(db, session).AsNoTracking()
            .Include(l => l.Subject).Include(l => l.Teacher).Include(l => l.Group)
            .Where(l => l.Date >= from && l.Date <= to)
            .OrderByDescending(l => l.Date).ThenBy(l => l.PairNumber)
            .ToListAsync(ct);
    }

    public async Task<AttendanceSheet> GetSheetAsync(int lessonId, CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var lesson = await FindLessonAsync(db, lessonId, ct);

        var absences = await db.Absences.AsNoTracking().Where(a => a.LessonId == lessonId).ToDictionaryAsync(a => a.StudentId, ct);
        var students = await db.Students.AsNoTracking().Where(s => s.GroupId == lesson.GroupId)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync(ct);

        var rows = students
            .Select(s => absences.TryGetValue(s.Id, out var a)
                ? new AttendanceRow(s.Id, s.FullName, a.Id, a.Status)
                : new AttendanceRow(s.Id, s.FullName, null, null))
            .ToList();
        var canChangeStatus = session.Role is UserRole.Admin or UserRole.Curator;
        return new AttendanceSheet(lesson, EditDeniedReason(lesson), canChangeStatus, rows);
    }

    public async Task<SaveSheetResult> SaveSheetAsync(int lessonId, IReadOnlyCollection<int> absentStudentIds, CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var lesson = await FindLessonAsync(db, lessonId, ct);
        if (EditDeniedReason(lesson) is { } reason)
            throw new ServiceException(reason);

        var groupStudentIds = await db.Students.Where(s => s.GroupId == lesson.GroupId).Select(s => s.Id).ToListAsync(ct);
        if (absentStudentIds.Except(groupStudentIds).Any())
            throw new ServiceException("В списке есть студент не из группы этого занятия.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var existing = await db.Absences.Where(a => a.LessonId == lessonId).ToListAsync(ct);
        var removed = existing.Where(a => !absentStudentIds.Contains(a.StudentId)).ToList();
        db.Absences.RemoveRange(removed);

        var newIds = absentStudentIds.Except(existing.Select(a => a.StudentId)).ToList();
        var coveringDocuments = await db.ExcuseDocuments
            .Where(d => newIds.Contains(d.StudentId) && d.Status == DocumentStatus.Approved
                        && d.ValidFrom <= lesson.Date && d.ValidTo >= lesson.Date)
            .Select(d => new { d.StudentId, d.Id })
            .ToListAsync(ct);

        var newUnexcused = new List<int>();
        foreach (var studentId in newIds)
        {
            var documentId = coveringDocuments.FirstOrDefault(d => d.StudentId == studentId)?.Id;
            db.Absences.Add(new Absence
            {
                StudentId = studentId,
                LessonId = lessonId,
                Status = documentId is null ? AbsenceStatus.Unexcused : AbsenceStatus.Excused,
                ExcuseDocumentId = documentId,
                MarkedByUserId = session.UserId
            });
            if (documentId is null)
                newUnexcused.Add(studentId);
        }

        await db.SaveChangesOrThrowAsync(ct);
        var exceeded = await CheckMonthlyLimitAsync(db, lesson, newUnexcused, ct);
        await transaction.CommitAsync(ct);
        return new SaveSheetResult(newIds.Count, removed.Count, exceeded);
    }

    public async Task ChangeStatusAsync(int absenceId, AbsenceStatus status, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin, UserRole.Curator);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var absence = await db.Absences
            .Where(a => AccessScope.Lessons(db, session).Any(l => l.Id == a.LessonId))
            .SingleOrDefaultAsync(a => a.Id == absenceId, ct)
            ?? throw new ServiceException("Пропуск не найден или относится не к вашей группе.");

        absence.Status = status;
        absence.ExcuseDocumentId = null;
        await db.SaveChangesOrThrowAsync(ct);
    }

    public async Task<IReadOnlyList<AbsenceInfo>> GetMyAbsencesAsync(CancellationToken ct = default)
    {
        session.Demand(UserRole.Student, UserRole.Headman);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Absences.AsNoTracking()
            .Where(a => a.Student!.UserId == session.UserId)
            .OrderByDescending(a => a.Lesson!.Date).ThenBy(a => a.Lesson!.PairNumber)
            .Select(a => new AbsenceInfo(a.Id, a.Lesson!.Date, a.Lesson.PairNumber, a.Lesson.Subject!.Name, a.Lesson.Hours, a.Status))
            .ToListAsync(ct);
    }

    private async Task<Lesson> FindLessonAsync(AttendanceDbContext db, int lessonId, CancellationToken ct) =>
        await AccessScope.Lessons(db, session).AsNoTracking()
            .Include(l => l.Subject).Include(l => l.Teacher).Include(l => l.Group)
            .SingleOrDefaultAsync(l => l.Id == lessonId, ct)
        ?? throw new ServiceException("Занятие не найдено или недоступно.");

    private string? EditDeniedReason(Lesson lesson)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (lesson.Date > today)
            return "Нельзя отмечать пропуски на занятиях, которые ещё не прошли.";
        return session.Role switch
        {
            UserRole.Admin or UserRole.Headman => null,
            UserRole.Teacher when lesson.Date == today => null,
            UserRole.Teacher => "Преподаватель может отмечать отсутствующих только в день занятия.",
            UserRole.Curator => "Куратор не отмечает присутствие, но может менять статус пропусков.",
            _ => "Недостаточно прав для отметки пропусков."
        };
    }

    private async Task<IReadOnlyList<string>> CheckMonthlyLimitAsync(AttendanceDbContext db, Lesson lesson,
        IReadOnlyCollection<int> studentIds, CancellationToken ct)
    {
        if (studentIds.Count == 0)
            return [];

        var limit = options.Value.MonthlyUnexcusedLimitHours;
        var monthStart = new DateOnly(lesson.Date.Year, lesson.Date.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var over = await db.Absences
            .Where(a => studentIds.Contains(a.StudentId) && a.Status == AbsenceStatus.Unexcused
                        && a.Lesson!.Date >= monthStart && a.Lesson.Date < nextMonth)
            .GroupBy(a => a.StudentId)
            .Select(g => new { StudentId = g.Key, Hours = g.Sum(a => (int)a.Lesson!.Hours) })
            .Where(x => x.Hours > limit)
            .ToListAsync(ct);
        if (over.Count == 0)
            return [];

        var overIds = over.Select(x => x.StudentId).ToList();
        var students = await db.Students.Where(s => overIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
        List<int> recipients = lesson.Group!.CuratorUserId is { } curatorId
            ? [curatorId]
            : await db.Users.Where(u => u.RoleId == UserRole.Admin).Select(u => u.Id).ToListAsync(ct);

        var since = monthStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var alreadyNotified = await db.Notifications
            .Where(n => n.StudentId != null && overIds.Contains(n.StudentId.Value) && n.CreatedAt >= since)
            .Select(n => n.StudentId!.Value)
            .ToListAsync(ct);

        foreach (var x in over.Where(x => !alreadyNotified.Contains(x.StudentId)))
        {
            var student = students[x.StudentId];
            foreach (var userId in recipients)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = userId,
                    StudentId = student.Id,
                    Message = $"Студент {student.LastName} {student.FirstName} ({lesson.Group.Name}): {x.Hours} ч неуважительных " +
                              $"пропусков за {monthStart:MM.yyyy} — превышен порог {limit} ч."
                });
            }
        }

        await db.SaveChangesOrThrowAsync(ct);
        return over.Select(x => $"{students[x.StudentId].FullName} — {x.Hours} ч").ToList();
    }
}
