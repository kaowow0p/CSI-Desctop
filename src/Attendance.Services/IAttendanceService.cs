using Attendance.Domain;
using Attendance.Domain.Entities;

namespace Attendance.Services;

public sealed record AttendanceRow(int StudentId, string StudentName, int? AbsenceId, AbsenceStatus? Status);

public sealed record AttendanceSheet(Lesson Lesson, string? ReadOnlyReason, bool CanChangeStatus, IReadOnlyList<AttendanceRow> Rows)
{
    public bool CanEdit => ReadOnlyReason is null;
}

public sealed record SaveSheetResult(int Added, int Removed, IReadOnlyList<string> LimitExceeded);

public sealed record AbsenceInfo(int Id, DateOnly Date, short PairNumber, string Subject, short Hours, AbsenceStatus Status);

public interface IAttendanceService
{
    Task<IReadOnlyList<Lesson>> GetLessonsAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<AttendanceSheet> GetSheetAsync(int lessonId, CancellationToken ct = default);

    Task<SaveSheetResult> SaveSheetAsync(int lessonId, IReadOnlyCollection<int> absentStudentIds, CancellationToken ct = default);

    Task ChangeStatusAsync(int absenceId, AbsenceStatus status, CancellationToken ct = default);

    Task<IReadOnlyList<AbsenceInfo>> GetMyAbsencesAsync(CancellationToken ct = default);
}
