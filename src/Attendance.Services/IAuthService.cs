namespace Attendance.Services;

public interface IAuthService
{
    Task LoginAsync(string login, string password, CancellationToken ct = default);

    Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default);

    void Logout();
}
