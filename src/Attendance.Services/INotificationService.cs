using Attendance.Domain.Entities;

namespace Attendance.Services;

/// <summary>Уведомления текущего пользователя.</summary>
public interface INotificationService
{
    Task<IReadOnlyList<Notification>> GetMyAsync(CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
}
