namespace Attendance.Domain.Entities;

public class Lesson
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public short PairNumber { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public int GroupId { get; set; }
    public Group? Group { get; set; }
    public short Hours { get; set; } = 2;

    public List<Absence> Absences { get; set; } = [];
}
