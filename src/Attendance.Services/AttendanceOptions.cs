namespace Attendance.Services;

/// <summary>Настройки из секции «Attendance» файла appsettings.json.</summary>
public sealed class AttendanceOptions
{
    /// <summary>Порог неуважительных пропусков за календарный месяц, после превышения которого уведомляется куратор.</summary>
    public int MonthlyUnexcusedLimitHours { get; set; } = 12;
}
