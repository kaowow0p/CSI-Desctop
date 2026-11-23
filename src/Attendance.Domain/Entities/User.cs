namespace Attendance.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserRole RoleId { get; set; }
    public Role? Role { get; set; }
    public int FailedLoginCount { get; set; }
    public bool IsLocked { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Teacher? Teacher { get; set; }
    public Student? Student { get; set; }
}
