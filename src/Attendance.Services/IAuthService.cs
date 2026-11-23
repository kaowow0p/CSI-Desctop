namespace Attendance.Services;

public interface IAuthService
{
    /// <summary>Проверяет логин и пароль и открывает сессию. При ошибке — <see cref="ServiceException"/>.</summary>
    Task LoginAsync(string login, string password, CancellationToken ct = default);

    Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default);

    void Logout();
}
