using Attendance.Data;
using Attendance.Domain;
using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static Attendance.Services.DbHelpers;

namespace Attendance.Services;

public sealed class UserService(IDbContextFactory<AttendanceDbContext> dbFactory, UserSession session) : IUserService
{
    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Users.AsNoTracking().Include(u => u.Role).OrderBy(u => u.Login).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);
    }

    public async Task<int> SaveUserAsync(User input, string? newPassword, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        var login = Required(input.Login, "Логин");
        var fullName = Required(input.FullName, "ФИО");
        if (!Enum.IsDefined(input.RoleId))
            throw new ServiceException("Выберите роль.");
        if (input.Id == 0 && string.IsNullOrEmpty(newPassword))
            throw new ServiceException("Для нового пользователя задайте временный пароль.");
        if (!string.IsNullOrEmpty(newPassword) && PasswordPolicy.Validate(newPassword) is { } passwordError)
            throw new ServiceException(passwordError);
        if (input.Id == session.UserId && (input.RoleId != UserRole.Admin || input.IsLocked))
            throw new ServiceException("Нельзя снять с себя роль администратора или заблокировать свою учётную запись.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.FindOrAddAsync(input.Id, ct);
        user.Login = login;
        user.FullName = fullName;
        user.RoleId = input.RoleId;
        if (user.IsLocked && !input.IsLocked)
            user.FailedLoginCount = 0;
        user.IsLocked = input.IsLocked;
        if (!string.IsNullOrEmpty(newPassword))
        {
            user.PasswordHash = AuthService.HashPassword(newPassword);
            user.MustChangePassword = true;
        }

        await db.SaveChangesOrThrowAsync(ct);
        return user.Id;
    }

    public async Task DeleteUserAsync(int id, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        if (id == session.UserId)
            throw new ServiceException("Нельзя удалить собственную учётную запись.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Users.Remove(await db.Users.FindExistingAsync(id, ct));
        await db.SaveChangesOrThrowAsync(ct);
    }
}
