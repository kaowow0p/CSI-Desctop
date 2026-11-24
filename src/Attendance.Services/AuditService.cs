using Attendance.Data;
using Attendance.Domain;
using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public interface IAuditService
{
    Task<IReadOnlyList<AuditLog>> GetAsync(DateOnly from, DateOnly to, string? tableName, CancellationToken ct = default);
}

public sealed class AuditService(IDbContextFactory<AttendanceDbContext> dbFactory, UserSession session) : IAuditService
{
    public static readonly string[] AuditedTables =
        ["users", "groups", "students", "teachers", "subjects", "lessons", "absences", "excuse_documents"];

    public const int MaxRows = 1000;

    public async Task<IReadOnlyList<AuditLog>> GetAsync(DateOnly from, DateOnly to, string? tableName, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        if (to < from)
            throw new ServiceException("Дата окончания периода раньше даты начала.");

        var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.AuditLog.AsNoTracking().Include(a => a.User)
            .Where(a => a.ChangedAt >= start && a.ChangedAt < end);
        if (tableName is not null)
            query = query.Where(a => a.TableName == tableName);

        return await query.OrderByDescending(a => a.ChangedAt).ThenByDescending(a => a.Id).Take(MaxRows).ToListAsync(ct);
    }
}
