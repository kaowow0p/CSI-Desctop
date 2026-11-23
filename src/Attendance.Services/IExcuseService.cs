using Attendance.Domain;
using Attendance.Domain.Entities;

namespace Attendance.Services;

/// <summary>
/// Оправдательные документы: староста регистрирует, куратор подтверждает или отклоняет.
/// После подтверждения пропуски студента за период действия документа становятся уважительными.
/// </summary>
public interface IExcuseService
{
    Task<IReadOnlyList<ExcuseDocument>> GetDocumentsAsync(DocumentStatus? status = null, CancellationToken ct = default);

    /// <summary>Студенты, для которых текущий пользователь может регистрировать документы.</summary>
    Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken ct = default);

    Task<int> RegisterAsync(ExcuseDocument document, CancellationToken ct = default);

    /// <summary>Подтверждает документ и возвращает число пропусков, ставших уважительными.</summary>
    Task<int> ApproveAsync(int id, CancellationToken ct = default);

    Task RejectAsync(int id, string comment, CancellationToken ct = default);

    /// <summary>Удаляет документ, ещё не прошедший проверку (например, зарегистрированный по ошибке).</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
