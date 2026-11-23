namespace Attendance.Domain.Entities;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Specialty { get; set; } = "";
    public short Course { get; set; }
    public int? CuratorUserId { get; set; }
    public User? Curator { get; set; }

    public List<Student> Students { get; set; } = [];
}
