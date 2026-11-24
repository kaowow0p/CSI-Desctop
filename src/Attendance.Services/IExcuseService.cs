using Attendance.Domain;
using Attendance.Domain.Entities;

namespace Attendance.Services;

public interface IExcuseService
{
    Task<IReadOnlyList<ExcuseDocument>> GetDocumentsAsync(DocumentStatus? status = null, CancellationToken ct = default);

    Task<IReadOnlyList<Student>> GetStudentsAsync(CancellationToken ct = default);

    Task<int> RegisterAsync(ExcuseDocument document, CancellationToken ct = default);

    Task<int> ApproveAsync(int id, CancellationToken ct = default);

    Task RejectAsync(int id, string comment, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
