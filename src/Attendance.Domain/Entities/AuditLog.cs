namespace Attendance.Domain.Entities;

/// <summary>Запись журнала аудита. Заполняется триггерами БД, приложение только читает.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = "";
    public string TableName { get; set; } = "";
    public long? RecordId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}
