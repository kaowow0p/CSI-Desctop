using Attendance.Data;
using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public sealed class NotificationService(IDbContextFactory<AttendanceDbContext> dbFactory, UserSession session) : INotificationService
{
    public async Task<IReadOnlyList<Notification>> GetMyAsync(CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Notifications.AsNoTracking()
            .Where(n => n.UserId == session.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Notifications.CountAsync(n => n.UserId == session.UserId && !n.IsRead, ct);
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Notifications
            .Where(n => n.UserId == session.UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
