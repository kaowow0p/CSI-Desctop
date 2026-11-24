using Attendance.Domain.Entities;

namespace Attendance.Services;

public interface IDirectoryService
{
    Task<IReadOnlyList<Group>> GetGroupsAsync(CancellationToken ct = default);
    Task<int> SaveGroupAsync(Group group, CancellationToken ct = default);
    Task DeleteGroupAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Student>> GetStudentsAsync(int? groupId = null, CancellationToken ct = default);
    Task<int> SaveStudentAsync(Student student, CancellationToken ct = default);
    Task DeleteStudentAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Teacher>> GetTeachersAsync(CancellationToken ct = default);
    Task<int> SaveTeacherAsync(Teacher teacher, CancellationToken ct = default);
    Task DeleteTeacherAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default);
    Task<int> SaveSubjectAsync(Subject subject, CancellationToken ct = default);
    Task DeleteSubjectAsync(int id, CancellationToken ct = default);
}
