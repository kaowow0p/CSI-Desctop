using System.Text.Encodings.Web;
using System.Text.Json;
using Attendance.Domain.Entities;
using Attendance.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Attendance.Desktop.ViewModels;

public partial class ReportsViewModel(IReportService reports, IDialogService dialogs) : ObservableObject, IPageViewModel
{
    [ObservableProperty] private IReadOnlyList<Group> _groups = [];
    [ObservableProperty] private Group? _selectedGroup;
    [ObservableProperty] private DateTime _from = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime _to = DateTime.Today;
    [ObservableProperty] private string _summary = "";
    [ObservableProperty] private string _errorMessage = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private AbsenceReport? _report;

    [RelayCommand]
    private async Task LoadAsync()
    {
        Groups = await reports.GetGroupsAsync();
        SelectedGroup = Groups.FirstOrDefault();
        if (SelectedGroup is null)
            ErrorMessage = "Нет доступных групп для построения отчёта.";
        else
            await BuildAsync();
    }

    [RelayCommand]
    private async Task BuildAsync()
    {
        ErrorMessage = Summary = "";
        if (SelectedGroup is not { } group)
            return;

        try
        {
            Report = await reports.GetAbsenceSummaryAsync(group.Id, DateOnly.FromDateTime(From), DateOnly.FromDateTime(To));
            Summary = $"Группа {Report.GroupName}: всего пропущено {Report.TotalHours} ч, из них неуважительных — {Report.UnexcusedHours} ч.";
        }
        catch (ServiceException ex)
        {
            Report = null;
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void Export()
    {
        var report = Report!;
        var path = dialogs.SaveFile($"Пропуски_{report.GroupName}_{report.From:yyyy-MM-dd}_{report.To:yyyy-MM-dd}.xlsx",
            "Книга Excel (*.xlsx)|*.xlsx");
        if (path is null)
            return;

        try
        {
            reports.ExportToExcel(report, path);
            dialogs.Info($"Отчёт сохранён: {path}");
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private bool CanExport() => Report is not null;
}

public partial class AuditViewModel(IAuditService audit) : ObservableObject, IPageViewModel
{
    private static readonly JsonSerializerOptions PrettyJson =
        new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static IReadOnlyList<Option<string?>> Tables { get; } =
        [new(null, "Все таблицы"), .. AuditService.AuditedTables.Select(t => new Option<string?>(t, t))];

    [ObservableProperty] private DateTime _from = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime _to = DateTime.Today;
    [ObservableProperty] private Option<string?> _table = Tables[0];
    [ObservableProperty] private IReadOnlyList<AuditLog> _items = [];
    [ObservableProperty] private AuditLog? _selected;
    [ObservableProperty] private string _oldValues = "";
    [ObservableProperty] private string _newValues = "";
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _errorMessage = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        ErrorMessage = Message = "";
        try
        {
            Items = await audit.GetAsync(DateOnly.FromDateTime(From), DateOnly.FromDateTime(To), Table.Value);
            Message = Items.Count == AuditService.MaxRows
                ? $"Показаны последние {AuditService.MaxRows} записей — сузьте период или выберите таблицу."
                : $"Записей: {Items.Count}.";
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    partial void OnSelectedChanged(AuditLog? value)
    {
        OldValues = Pretty(value?.OldValues);
        NewValues = Pretty(value?.NewValues);
    }

    private static string Pretty(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return "—";
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, PrettyJson);
    }
}
