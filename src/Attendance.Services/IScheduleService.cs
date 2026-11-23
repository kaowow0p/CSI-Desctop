using Attendance.Domain.Entities;

namespace Attendance.Services;

/// <summary>Строка шаблона недели: в этот день недели на этой паре — такая дисциплина у такого преподавателя.</summary>
public sealed record ScheduleTemplateItem(DayOfWeek Day, short PairNumber, int SubjectId, int TeacherId, short Hours);

public sealed record GenerationResult(int Created, int Skipped);

public interface IScheduleService
{
    Task<IReadOnlyList<Lesson>> GetLessonsAsync(int groupId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<int> SaveLessonAsync(Lesson lesson, CancellationToken ct = default);

    /// <summary>Удаляет занятие вместе с отметками о пропусках на нём.</summary>
    Task DeleteLessonAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Создаёт занятия группы на период, повторяя шаблон каждую неделю.
    /// Пары, которые у группы уже заняты, пропускаются.
    /// </summary>
    Task<GenerationResult> GenerateAsync(int groupId, DateOnly from, DateOnly to,
        IReadOnlyCollection<ScheduleTemplateItem> template, CancellationToken ct = default);
}
