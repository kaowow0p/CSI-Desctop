using Attendance.Data;
using Attendance.Domain;
using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public sealed class ScheduleService(IDbContextFactory<AttendanceDbContext> dbFactory, UserSession session) : IScheduleService
{
    public const int MaxPair = 7;
    private const int MaxGenerationDays = 366;

    public async Task<IReadOnlyList<Lesson>> GetLessonsAsync(int groupId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Lessons.AsNoTracking()
            .Include(l => l.Subject).Include(l => l.Teacher)
            .Where(l => l.GroupId == groupId && l.Date >= from && l.Date <= to)
            .OrderBy(l => l.Date).ThenBy(l => l.PairNumber)
            .ToListAsync(ct);
    }

    public async Task<int> SaveLessonAsync(Lesson input, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        Validate(input.PairNumber, input.Hours);
        if (input.GroupId == 0 || input.SubjectId == 0 || input.TeacherId == 0)
            throw new ServiceException("Укажите группу, дисциплину и преподавателя.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var lesson = await db.Lessons.FindOrAddAsync(input.Id, ct);
        lesson.Date = input.Date;
        lesson.PairNumber = input.PairNumber;
        lesson.GroupId = input.GroupId;
        lesson.SubjectId = input.SubjectId;
        lesson.TeacherId = input.TeacherId;
        lesson.Hours = input.Hours;
        await db.SaveChangesOrThrowAsync(ct);
        return lesson.Id;
    }

    public async Task DeleteLessonAsync(int id, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Lessons.Remove(await db.Lessons.FindExistingAsync(id, ct));
        await db.SaveChangesOrThrowAsync(ct);
    }

    public async Task<GenerationResult> GenerateAsync(int groupId, DateOnly from, DateOnly to,
        IReadOnlyCollection<ScheduleTemplateItem> template, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        if (groupId == 0)
            throw new ServiceException("Выберите группу.");
        if (template.Count == 0)
            throw new ServiceException("Шаблон недели пуст.");
        if (to < from)
            throw new ServiceException("Дата окончания периода раньше даты начала.");
        if (to.DayNumber - from.DayNumber >= MaxGenerationDays)
            throw new ServiceException("Период генерации не может превышать один год.");
        foreach (var item in template)
            Validate(item.PairNumber, item.Hours);
        if (template.GroupBy(t => (t.Day, t.PairNumber)).Any(g => g.Count() > 1))
            throw new ServiceException("В шаблоне есть две записи на одну и ту же пару одного дня.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var busy = (await db.Lessons
                .Where(l => l.GroupId == groupId && l.Date >= from && l.Date <= to)
                .Select(l => new { l.Date, l.PairNumber })
                .ToListAsync(ct))
            .Select(l => (l.Date, l.PairNumber))
            .ToHashSet();

        int created = 0, skipped = 0;
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var item in template.Where(t => t.Day == date.DayOfWeek))
            {
                if (busy.Contains((date, item.PairNumber)))
                {
                    skipped++;
                    continue;
                }

                db.Lessons.Add(new Lesson
                {
                    Date = date,
                    PairNumber = item.PairNumber,
                    GroupId = groupId,
                    SubjectId = item.SubjectId,
                    TeacherId = item.TeacherId,
                    Hours = item.Hours
                });
                created++;
            }
        }

        await db.SaveChangesOrThrowAsync(ct);
        return new GenerationResult(created, skipped);
    }

    private static void Validate(short pairNumber, short hours)
    {
        if (pairNumber is < 1 or > MaxPair)
            throw new ServiceException($"Номер пары должен быть от 1 до {MaxPair}.");
        if (hours <= 0)
            throw new ServiceException("Количество часов должно быть больше нуля.");
    }
}
