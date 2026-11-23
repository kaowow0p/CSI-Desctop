namespace Attendance.Domain.Entities;

/// <summary>Оправдательный документ. Медицинские диагнозы намеренно не хранятся.</summary>
public class ExcuseDocument
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public DocumentType Type { get; set; }
    public string Number { get; set; } = "";
    public DateOnly IssueDate { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    public DocumentStatus Status { get; set; }
    public int RegisteredByUserId { get; set; }
    public User? RegisteredBy { get; set; }
    public DateTime RegisteredAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }
}
