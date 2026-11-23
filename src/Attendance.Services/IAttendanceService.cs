using Attendance.Domain;
using Attendance.Domain.Entities;

namespace Attendance.Services;

/// <summary>Строка ведомости: студент группы и его отметка на занятии (если есть).</summary>
public sealed record AttendanceRow(int StudentId, string StudentName, int? AbsenceId, AbsenceStatus? Status);

/// <param name="ReadOnlyReason">Почему текущий пользователь не может отмечать (null — может).</param>
public sealed record AttendanceSheet(Lesson Lesson, string? ReadOnlyReason, bool CanChangeStatus, IReadOnlyList<AttendanceRow> Rows)
{
    public bool CanEdit => ReadOnlyReason is null;
}

/// <param name="LimitExceeded">Студенты, у которых после сохранения превышен месячный лимит неуважительных часов.</param>
public sealed record SaveSheetResult(int Added, int Removed, IReadOnlyList<string> LimitExceeded);

public sealed record AbsenceInfo(int Id, DateOnly Date, short PairNumber, string Subject, short Hours, AbsenceStatus Status);

public interface IAttendanceService
{
    /// <summary>Занятия за период, доступные текущему пользователю.</summary>
    Task<IReadOnlyList<Lesson>> GetLessonsAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<AttendanceSheet> GetSheetAsync(int lessonId, CancellationToken ct = default);

    /// <summary>
    /// Сохраняет список отсутствующих на занятии. Новые пропуски, покрытые подтверждённой справкой,
    /// сразу становятся уважительными. После сохранения проверяется месячный лимит и создаются уведомления куратору.
    /// </summary>
    Task<SaveSheetResult> SaveSheetAsync(int lessonId, IReadOnlyCollection<int> absentStudentIds, CancellationToken ct = default);

    /// <summary>Ручная смена статуса пропуска (куратор своей группы, администратор).</summary>
    Task ChangeStatusAsync(int absenceId, AbsenceStatus status, CancellationToken ct = default);

    /// <summary>Пропуски текущего студента.</summary>
    Task<IReadOnlyList<AbsenceInfo>> GetMyAbsencesAsync(CancellationToken ct = default);
}
