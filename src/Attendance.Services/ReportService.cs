using Attendance.Data;
using Attendance.Domain;
using Attendance.Domain.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public sealed record AbsenceSummaryRow(string Student, int Pairs, int TotalHours, int ExcusedHours, int UnexcusedHours);

public sealed record AbsenceReport(string GroupName, DateOnly From, DateOnly To, IReadOnlyList<AbsenceSummaryRow> Rows)
{
    public int TotalHours => Rows.Sum(r => r.TotalHours);
    public int UnexcusedHours => Rows.Sum(r => r.UnexcusedHours);
}

/// <summary>Отчёты о посещаемости. Учитываются только занятия, доступные пользователю (преподаватель — свои).</summary>
public interface IReportService
{
    /// <summary>Группы, по которым пользователь может строить отчёт.</summary>
    Task<IReadOnlyList<Group>> GetGroupsAsync(CancellationToken ct = default);

    /// <summary>Сводка пропусков по студентам группы за период; сверху — больше всего неуважительных часов.</summary>
    Task<AbsenceReport> GetAbsenceSummaryAsync(int groupId, DateOnly from, DateOnly to, CancellationToken ct = default);

    void ExportToExcel(AbsenceReport report, string path);
}

public sealed class ReportService(IDbContextFactory<AttendanceDbContext> dbFactory, UserSession session) : IReportService
{
    public async Task<IReadOnlyList<Group>> GetGroupsAsync(CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await AccessScope.Groups(db, session).AsNoTracking().OrderBy(g => g.Name).ToListAsync(ct);
    }

    public async Task<AbsenceReport> GetAbsenceSummaryAsync(int groupId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        session.Demand(UserSession.Staff);
        if (to < from)
            throw new ServiceException("Дата окончания периода раньше даты начала.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var group = await AccessScope.Groups(db, session).AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId, ct)
            ?? throw new ServiceException("Группа не найдена или недоступна.");

        var lessons = AccessScope.Lessons(db, session).Where(l => l.GroupId == groupId && l.Date >= from && l.Date <= to);
        var totals = await db.Absences
            .Where(a => lessons.Any(l => l.Id == a.LessonId))
            .GroupBy(a => a.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                Pairs = g.Count(),
                Total = g.Sum(a => (int)a.Lesson!.Hours),
                Excused = g.Sum(a => a.Status == AbsenceStatus.Excused ? (int)a.Lesson!.Hours : 0)
            })
            .ToDictionaryAsync(x => x.StudentId, ct);

        var students = await db.Students.AsNoTracking().Where(s => s.GroupId == groupId).ToListAsync(ct);
        var rows = students
            .Select(s => totals.TryGetValue(s.Id, out var t)
                ? new AbsenceSummaryRow(s.FullName, t.Pairs, t.Total, t.Excused, t.Total - t.Excused)
                : new AbsenceSummaryRow(s.FullName, 0, 0, 0, 0))
            .OrderByDescending(r => r.UnexcusedHours).ThenByDescending(r => r.TotalHours).ThenBy(r => r.Student)
            .ToList();

        return new AbsenceReport(group.Name, from, to, rows);
    }

    public void ExportToExcel(AbsenceReport report, string path)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Пропуски");

        sheet.Cell(1, 1).Value = $"Сводка пропусков группы {report.GroupName} за период {report.From:dd.MM.yyyy} – {report.To:dd.MM.yyyy}";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        string[] headers = ["№", "Студент", "Пропущено пар", "Всего часов", "Уважительные, ч", "Неуважительные, ч"];
        for (var c = 0; c < headers.Length; c++)
            sheet.Cell(3, c + 1).Value = headers[c];

        var row = 4;
        foreach (var r in report.Rows)
        {
            sheet.Cell(row, 1).Value = row - 3;
            sheet.Cell(row, 2).Value = r.Student;
            sheet.Cell(row, 3).Value = r.Pairs;
            sheet.Cell(row, 4).Value = r.TotalHours;
            sheet.Cell(row, 5).Value = r.ExcusedHours;
            sheet.Cell(row, 6).Value = r.UnexcusedHours;
            row++;
        }

        sheet.Cell(row, 2).Value = "Итого";
        sheet.Cell(row, 3).Value = report.Rows.Sum(r => r.Pairs);
        sheet.Cell(row, 4).Value = report.TotalHours;
        sheet.Cell(row, 5).Value = report.Rows.Sum(r => r.ExcusedHours);
        sheet.Cell(row, 6).Value = report.UnexcusedHours;

        var table = sheet.Range(3, 1, row, headers.Length);
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sheet.Range(3, 1, 3, headers.Length).Style.Font.Bold = true;
        sheet.Range(3, 1, 3, headers.Length).Style.Fill.BackgroundColor = XLColor.LightGray;
        sheet.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        try
        {
            workbook.SaveAs(path);
        }
        catch (IOException ex)
        {
            throw new ServiceException($"Не удалось сохранить файл (возможно, он открыт в Excel): {ex.Message}");
        }
    }
}
