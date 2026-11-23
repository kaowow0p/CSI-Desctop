using Attendance.Domain;
using Attendance.Domain.Entities;

namespace Attendance.Services;

/// <summary>Текущий вошедший пользователь (один на экземпляр приложения).</summary>
public sealed class UserSession
{
    public int UserId { get; private set; }
    public string Login { get; private set; } = "";
    public string FullName { get; private set; } = "";
    public UserRole Role { get; private set; }
    public string RoleName { get; private set; } = "";
    public bool MustChangePassword { get; internal set; }

    public bool IsAuthenticated => UserId != 0;

    /// <summary>Сотрудники колледжа (все роли, кроме студента).</summary>
    public static readonly UserRole[] Staff = [UserRole.Admin, UserRole.Curator, UserRole.Teacher, UserRole.Headman];

    /// <summary>Проверка прав: пользователь вошёл и имеет одну из указанных ролей.</summary>
    public void Demand(params UserRole[] roles)
    {
        if (!IsAuthenticated)
            throw new ServiceException("Сначала выполните вход.");
        if (!roles.Contains(Role))
            throw new ServiceException("Недостаточно прав для выполнения операции.");
    }

    internal void Start(User user)
    {
        UserId = user.Id;
        Login = user.Login;
        FullName = user.FullName;
        Role = user.RoleId;
        RoleName = user.Role?.Name ?? user.RoleId.ToString();
        MustChangePassword = user.MustChangePassword;
    }

    internal void Clear()
    {
        UserId = 0;
        Login = FullName = RoleName = "";
        MustChangePassword = false;
    }
}
