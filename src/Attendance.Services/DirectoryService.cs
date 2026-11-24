using Attendance.Data;
using Attendance.Domain;
using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static Attendance.Services.DbHelpers;

namespace Attendance.Services;

public sealed class DirectoryService(IDbContextFactory<AttendanceDbContext> dbFactory, UserSession session) : IDirectoryService
{
    public async Task<IReadOnlyList<Group>> GetGroupsAsync(CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Groups.AsNoTracking().Include(g => g.Curator).OrderBy(g => g.Name).ToListAsync(ct);
    }

    public async Task<int> SaveGroupAsync(Group input, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        var name = Required(input.Name, "Название");
        var specialty = Required(input.Specialty, "Специальность");
        if (input.Course is < 1 or > 6)
            throw new ServiceException("Курс должен быть от 1 до 6.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await EnsureUserRoleAsync(db, input.CuratorUserId, "Куратором может быть только пользователь с ролью «Куратор».", ct, UserRole.Curator);

        var group = await db.Groups.FindOrAddAsync(input.Id, ct);
        group.Name = name;
        group.Specialty = specialty;
        group.Course = input.Course;
        group.CuratorUserId = input.CuratorUserId;
        await db.SaveChangesOrThrowAsync(ct);
        return group.Id;
    }

    public async Task DeleteGroupAsync(int id, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Groups.Remove(await db.Groups.FindExistingAsync(id, ct));
        await db.SaveChangesOrThrowAsync(ct);
    }

    public async Task<IReadOnlyList<Student>> GetStudentsAsync(int? groupId = null, CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Students.AsNoTracking()
            .Include(s => s.Group).Include(s => s.User)
            .Where(s => groupId == null || s.GroupId == groupId)
            .OrderBy(s => s.Group!.Name).ThenBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync(ct);
    }

    public async Task<int> SaveStudentAsync(Student input, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        var lastName = Required(input.LastName, "Фамилия");
        var firstName = Required(input.FirstName, "Имя");
        var recordBook = Required(input.RecordBookNumber, "Номер зачётной книжки");
        if (input.GroupId == 0)
            throw new ServiceException("Выберите группу.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await EnsureUserRoleAsync(db, input.UserId, "К студенту можно привязать только учётную запись студента или старосты.", ct,
            UserRole.Student, UserRole.Headman);

        var student = await db.Students.FindOrAddAsync(input.Id, ct);
        student.LastName = lastName;
        student.FirstName = firstName;
        student.MiddleName = Optional(input.MiddleName);
        student.RecordBookNumber = recordBook;
        student.GroupId = input.GroupId;
        student.UserId = input.UserId;
        await db.SaveChangesOrThrowAsync(ct);
        return student.Id;
    }

    public async Task DeleteStudentAsync(int id, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Students.Remove(await db.Students.FindExistingAsync(id, ct));
        await db.SaveChangesOrThrowAsync(ct);
    }

    public async Task<IReadOnlyList<Teacher>> GetTeachersAsync(CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Teachers.AsNoTracking().Include(t => t.User)
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .ToListAsync(ct);
    }

    public async Task<int> SaveTeacherAsync(Teacher input, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        var lastName = Required(input.LastName, "Фамилия");
        var firstName = Required(input.FirstName, "Имя");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await EnsureUserRoleAsync(db, input.UserId, "К преподавателю можно привязать только учётную запись с ролью «Преподаватель».", ct,
            UserRole.Teacher);

        var teacher = await db.Teachers.FindOrAddAsync(input.Id, ct);
        teacher.LastName = lastName;
        teacher.FirstName = firstName;
        teacher.MiddleName = Optional(input.MiddleName);
        teacher.UserId = input.UserId;
        await db.SaveChangesOrThrowAsync(ct);
        return teacher.Id;
    }

    public async Task DeleteTeacherAsync(int id, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Teachers.Remove(await db.Teachers.FindExistingAsync(id, ct));
        await db.SaveChangesOrThrowAsync(ct);
    }

    public async Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Subjects.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<int> SaveSubjectAsync(Subject input, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        var name = Required(input.Name, "Название");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var subject = await db.Subjects.FindOrAddAsync(input.Id, ct);
        subject.Name = name;
        await db.SaveChangesOrThrowAsync(ct);
        return subject.Id;
    }

    public async Task DeleteSubjectAsync(int id, CancellationToken ct = default)
    {
        session.Demand(UserRole.Admin);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Subjects.Remove(await db.Subjects.FindExistingAsync(id, ct));
        await db.SaveChangesOrThrowAsync(ct);
    }

    private static async Task EnsureUserRoleAsync(AttendanceDbContext db, int? userId, string error, CancellationToken ct,
        params UserRole[] roles)
    {
        if (userId is { } id && !await db.Users.AnyAsync(u => u.Id == id && roles.Contains(u.RoleId), ct))
            throw new ServiceException(error);
    }
}
