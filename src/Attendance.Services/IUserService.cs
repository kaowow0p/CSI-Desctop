using Attendance.Domain.Entities;

namespace Attendance.Services;

public interface IUserService
{
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default);

    Task<int> SaveUserAsync(User user, string? newPassword, CancellationToken ct = default);

    Task DeleteUserAsync(int id, CancellationToken ct = default);
}
