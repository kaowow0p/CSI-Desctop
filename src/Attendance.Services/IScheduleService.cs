using Attendance.Domain.Entities;

namespace Attendance.Services;

public sealed record ScheduleTemplateItem(DayOfWeek Day, short PairNumber, int SubjectId, int TeacherId, short Hours);

public sealed record GenerationResult(int Created, int Skipped);

public interface IScheduleService
{
    Task<IReadOnlyList<Lesson>> GetLessonsAsync(int groupId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<int> SaveLessonAsync(Lesson lesson, CancellationToken ct = default);

    Task DeleteLessonAsync(int id, CancellationToken ct = default);

    Task<GenerationResult> GenerateAsync(int groupId, DateOnly from, DateOnly to,
        IReadOnlyCollection<ScheduleTemplateItem> template, CancellationToken ct = default);
}
