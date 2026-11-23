namespace Attendance.Domain.Entities;

public class Absence
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public AbsenceStatus Status { get; set; }
    public int MarkedByUserId { get; set; }
    public User? MarkedBy { get; set; }
    public DateTime MarkedAt { get; set; }

    /// <summary>Документ, по которому пропуск признан уважительным.</summary>
    public int? ExcuseDocumentId { get; set; }
    public ExcuseDocument? ExcuseDocument { get; set; }
}
