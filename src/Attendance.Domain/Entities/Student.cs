namespace Attendance.Domain.Entities;

public class Student
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Group? Group { get; set; }
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }
    public string RecordBookNumber { get; set; } = "";
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string FullName => $"{LastName} {FirstName} {MiddleName}".TrimEnd();
}
