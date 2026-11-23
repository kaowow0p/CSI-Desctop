using Attendance.Data;
using Attendance.Domain;
using Attendance.Domain.Entities;

namespace Attendance.Services;

/// <summary>Какие данные видит текущий пользователь в зависимости от роли.</summary>
internal static class AccessScope
{
    /// <summary>Занятия: администратор — все, куратор — своих групп, преподаватель — свои, староста — своей группы.</summary>
    public static IQueryable<Lesson> Lessons(AttendanceDbContext db, UserSession session) => session.Role switch
    {
        UserRole.Admin => db.Lessons,
        UserRole.Curator => db.Lessons.Where(l => l.Group!.CuratorUserId == session.UserId),
        UserRole.Teacher => db.Lessons.Where(l => l.Teacher!.UserId == session.UserId),
        UserRole.Headman => db.Lessons.Where(l => db.Students.Any(s => s.UserId == session.UserId && s.GroupId == l.GroupId)),
        _ => db.Lessons.Where(_ => false)
    };

    /// <summary>Группы: администратор — все, куратор — свои, преподаватель — где ведёт занятия, староста — своя.</summary>
    public static IQueryable<Group> Groups(AttendanceDbContext db, UserSession session) => session.Role switch
    {
        UserRole.Admin => db.Groups,
        UserRole.Curator => db.Groups.Where(g => g.CuratorUserId == session.UserId),
        UserRole.Teacher => db.Groups.Where(g => db.Lessons.Any(l => l.GroupId == g.Id && l.Teacher!.UserId == session.UserId)),
        UserRole.Headman => db.Groups.Where(g => db.Students.Any(s => s.UserId == session.UserId && s.GroupId == g.Id)),
        _ => db.Groups.Where(_ => false)
    };

    /// <summary>Студенты: администратор — все, куратор — своих групп, староста — своей группы, студент — только он сам.</summary>
    public static IQueryable<Student> Students(AttendanceDbContext db, UserSession session) => session.Role switch
    {
        UserRole.Admin => db.Students,
        UserRole.Curator => db.Students.Where(s => s.Group!.CuratorUserId == session.UserId),
        UserRole.Headman => db.Students.Where(s => db.Students.Any(h => h.UserId == session.UserId && h.GroupId == s.GroupId)),
        UserRole.Student => db.Students.Where(s => s.UserId == session.UserId),
        _ => db.Students.Where(_ => false)
    };
}
