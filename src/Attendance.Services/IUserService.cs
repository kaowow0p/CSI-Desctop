using Attendance.Domain.Entities;

namespace Attendance.Services;

/// <summary>Учётные записи пользователей (только администратор).</summary>
public interface IUserService
{
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default);

    /// <summary>
    /// Создаёт или изменяет пользователя. Если задан <paramref name="newPassword"/>, пароль сбрасывается
    /// и пользователь обязан сменить его при входе. Для нового пользователя пароль обязателен.
    /// </summary>
    Task<int> SaveUserAsync(User user, string? newPassword, CancellationToken ct = default);

    Task DeleteUserAsync(int id, CancellationToken ct = default);
}
