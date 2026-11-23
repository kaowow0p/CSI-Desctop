using Attendance.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Attendance.Services;

internal static class DbHelpers
{
    private static readonly Dictionary<string, string> UniqueMessages = new()
    {
        ["ix_users_login"] = "Пользователь с таким логином уже существует.",
        ["ix_groups_name"] = "Группа с таким названием уже существует.",
        ["ix_students_record_book_number"] = "Студент с таким номером зачётной книжки уже есть.",
        ["ix_students_user_id"] = "Эта учётная запись уже привязана к другому студенту.",
        ["ix_teachers_user_id"] = "Эта учётная запись уже привязана к другому преподавателю.",
        ["ix_subjects_name"] = "Дисциплина с таким названием уже существует.",
        ["ix_lessons_group_id_date_pair_number"] = "У группы уже есть занятие на этой паре в этот день.",
        ["ix_absences_student_id_lesson_id"] = "Пропуск этого студента на занятии уже отмечен.",
    };

    /// <summary>SaveChanges с переводом ошибок PostgreSQL в понятные пользователю сообщения.</summary>
    public static async Task SaveChangesOrThrowAsync(this AttendanceDbContext db, CancellationToken ct = default)
    {
        var deleting = db.ChangeTracker.Entries().Any(e => e.State == EntityState.Deleted);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            throw new ServiceException(pg.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation =>
                    UniqueMessages.GetValueOrDefault(pg.ConstraintName ?? "", "Такая запись уже существует."),
                PostgresErrorCodes.ForeignKeyViolation => deleting
                    ? "Нельзя удалить запись: на неё ссылаются другие данные."
                    : "Связанная запись не найдена (возможно, её удалили).",
                PostgresErrorCodes.CheckViolation => $"Данные не прошли проверку ({pg.ConstraintName}).",
                PostgresErrorCodes.StringDataRightTruncation => "Одно из значений слишком длинное.",
                _ => $"Ошибка базы данных: {pg.MessageText}"
            });
        }
    }

    /// <summary>Новая сущность (id = 0) или существующая из БД.</summary>
    public static async Task<T> FindOrAddAsync<T>(this DbSet<T> set, int id, CancellationToken ct) where T : class, new() =>
        id == 0 ? set.Add(new T()).Entity : await set.FindExistingAsync(id, ct);

    public static async Task<T> FindExistingAsync<T>(this DbSet<T> set, int id, CancellationToken ct) where T : class =>
        await set.FindAsync([id], ct) ?? throw new ServiceException("Запись не найдена — возможно, её удалил другой пользователь.");

    public static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new ServiceException($"Заполните поле «{field}».") : value.Trim();

    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
