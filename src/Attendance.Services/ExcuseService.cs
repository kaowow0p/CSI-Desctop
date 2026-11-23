using Attendance.Data;
using Attendance.Domain;
using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static Attendance.Services.DbHelpers;

namespace Attendance.Services;

public sealed class ExcuseService(IDbContextFactory<AttendanceDbContext> dbFactory, UserSession session) : IExcuseService
{
    private static readonly UserRole[] Registrars = [UserRole.Admin, UserRole.Headman];
    private static readonly UserRole[] Reviewers = [UserRole.Admin, UserRole.Curator];

    public async Task<IReadOnlyList<ExcuseDocument>> GetDocumentsAsync(DocumentStatus? status = null, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin, UserRole.Curator, UserRole.Headman);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await VisibleDocuments(db).AsNoTracking()
            .Include(d => d.Student).Include(d => d.RegisteredBy).Include(d => d.ReviewedBy)
            .Where(d => status == null || d.Status == status)
            .OrderByDescending(d => d.RegisteredAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken ct = default)
    {
        session.Demand(Registrars);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await AccessScope.Students(db, session).AsNoTracking()
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync(ct);
    }

    public async Task<int> RegisterAsync(ExcuseDocument input, CancellationToken ct = default)
    {
        session.Demand(Registrars);
        var number = Required(input.Number, "Номер документа");
        if (!Enum.IsDefined(input.Type))
            throw new ServiceException("Выберите тип документа.");
        if (input.ValidTo < input.ValidFrom)
            throw new ServiceException("Дата окончания действия не может быть раньше даты начала.");
        if (input.IssueDate > DateOnly.FromDateTime(DateTime.Now))
            throw new ServiceException("Дата выдачи не может быть в будущем.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!await AccessScope.Students(db, session).AnyAsync(s => s.Id == input.StudentId, ct))
            throw new ServiceException("Выберите студента своей группы.");

        var document = new ExcuseDocument
        {
            StudentId = input.StudentId,
            Type = input.Type,
            Number = number,
            IssueDate = input.IssueDate,
            ValidFrom = input.ValidFrom,
            ValidTo = input.ValidTo,
            Status = DocumentStatus.Pending,
            RegisteredByUserId = session.UserId
        };
        db.ExcuseDocuments.Add(document);
        await db.SaveChangesOrThrowAsync(ct);
        return document.Id;
    }

    public async Task<int> ApproveAsync(int id, CancellationToken ct = default)
    {
        session.Demand(Reviewers);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var document = await FindPendingAsync(db, id, ct);
        document.Status = DocumentStatus.Approved;
        document.ReviewedByUserId = session.UserId;
        document.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesOrThrowAsync(ct);

        var excused = await db.Absences
            .Where(a => a.StudentId == document.StudentId && a.Status == AbsenceStatus.Unexcused
                        && a.Lesson!.Date >= document.ValidFrom && a.Lesson.Date <= document.ValidTo)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Status, AbsenceStatus.Excused)
                .SetProperty(a => a.ExcuseDocumentId, document.Id), ct);

        await transaction.CommitAsync(ct);
        return excused;
    }

    public async Task RejectAsync(int id, string comment, CancellationToken ct = default)
    {
        session.Demand(Reviewers);
        var reason = Required(comment, "Причина отклонения");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var document = await FindPendingAsync(db, id, ct);
        document.Status = DocumentStatus.Rejected;
        document.ReviewedByUserId = session.UserId;
        document.ReviewedAt = DateTime.UtcNow;
        document.ReviewComment = reason;
        await db.SaveChangesOrThrowAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        session.Demand(Registrars);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.ExcuseDocuments.Remove(await FindPendingAsync(db, id, ct));
        await db.SaveChangesOrThrowAsync(ct);
    }

    private IQueryable<ExcuseDocument> VisibleDocuments(AttendanceDbContext db) =>
        db.ExcuseDocuments.Where(d => AccessScope.Students(db, session).Any(s => s.Id == d.StudentId));

    private async Task<ExcuseDocument> FindPendingAsync(AttendanceDbContext db, int id, CancellationToken ct)
    {
        var document = await VisibleDocuments(db).SingleOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new ServiceException("Документ не найден или относится не к вашей группе.");
        return document.Status == DocumentStatus.Pending
            ? document
            : throw new ServiceException("Документ уже проверен — изменить его нельзя.");
    }
}
