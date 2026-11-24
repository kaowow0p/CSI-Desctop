using Attendance.Domain;
using Attendance.Domain.Entities;

namespace Attendance.Services;

public sealed class UserSession
{
    public int UserId { get; private set; }
    public string Login { get; private set; } = "";
    public string FullName { get; private set; } = "";
    public UserRole Role { get; private set; }
    public string RoleName { get; private set; } = "";
    public bool MustChangePassword { get; internal set; }

    public bool IsAuthenticated => UserId != 0;

    public static readonly UserRole[] Staff = [UserRole.Admin, UserRole.Curator, UserRole.Teacher, UserRole.Headman];

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
