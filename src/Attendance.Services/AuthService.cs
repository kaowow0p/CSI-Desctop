using Attendance.Data;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public sealed class AuthService(IDbContextFactory<AttendanceDbContext> dbFactory, UserSession session) : IAuthService
{
    public const int MaxFailedAttempts = 5;
    private const int BcryptWorkFactor = 11;
    private const string InvalidCredentials = "Неверный логин или пароль.";
    private const string LockedMessage = "Учётная запись заблокирована. Обратитесь к администратору.";

    public async Task LoginAsync(string login, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
            throw new ServiceException("Введите логин и пароль.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Login == login.Trim(), ct)
            ?? throw new ServiceException(InvalidCredentials);

        if (user.IsLocked)
            throw new ServiceException(LockedMessage);

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            user.IsLocked = user.FailedLoginCount >= MaxFailedAttempts;
            await db.SaveChangesAsync(ct);
            throw new ServiceException(user.IsLocked
                ? $"{InvalidCredentials} {LockedMessage}"
                : $"{InvalidCredentials} Осталось попыток: {MaxFailedAttempts - user.FailedLoginCount}.");
        }

        if (user.FailedLoginCount > 0)
        {
            user.FailedLoginCount = 0;
            await db.SaveChangesAsync(ct);
        }

        session.Start(user);
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        if (!session.IsAuthenticated)
            throw new ServiceException("Сначала выполните вход.");
        if (PasswordPolicy.Validate(newPassword) is { } error)
            throw new ServiceException(error);
        if (newPassword == currentPassword)
            throw new ServiceException("Новый пароль должен отличаться от текущего.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.SingleAsync(u => u.Id == session.UserId, ct);
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new ServiceException("Текущий пароль указан неверно.");

        user.PasswordHash = HashPassword(newPassword);
        user.MustChangePassword = false;
        await db.SaveChangesAsync(ct);
        session.MustChangePassword = false;
    }

    public void Logout() => session.Clear();

    public static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor);
}
