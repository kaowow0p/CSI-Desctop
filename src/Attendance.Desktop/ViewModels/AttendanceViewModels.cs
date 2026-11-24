using Attendance.Domain;
using Attendance.Domain.Entities;
using Attendance.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Attendance.Desktop.ViewModels;

public sealed record Option<T>(T Value, string Name);

public partial class AttendanceRowViewModel(AttendanceRow row, bool canChangeStatus) : ObservableObject
{
    public int StudentId => row.StudentId;
    public string Name => row.StudentName;
    public int? AbsenceId => row.AbsenceId;
    public AbsenceStatus? Status => row.Status;
    public bool CanToggleStatus => canChangeStatus && row.AbsenceId is not null;

    [ObservableProperty]
    private bool _isAbsent = row.AbsenceId is not null;
}

public partial class JournalViewModel(IAttendanceService attendance) : ObservableObject, IPageViewModel
{
    [ObservableProperty] private DateTime _from = DateTime.Today.AddDays(-14);
    [ObservableProperty] private DateTime _to = DateTime.Today;
    [ObservableProperty] private IReadOnlyList<Lesson> _lessons = [];
    [ObservableProperty] private Lesson? _selectedLesson;
    [ObservableProperty] private IReadOnlyList<AttendanceRowViewModel> _rows = [];
    [ObservableProperty] private string _lessonTitle = "Выберите занятие слева";
    [ObservableProperty] private string _readOnlyReason = "";
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _errorMessage = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _canEdit;

    [RelayCommand]
    private async Task LoadAsync()
    {
        Lessons = await attendance.GetLessonsAsync(DateOnly.FromDateTime(From), DateOnly.FromDateTime(To));
        SelectedLesson = Lessons.FirstOrDefault();
        if (SelectedLesson is null)
            await LoadSheetAsync();
    }

    partial void OnSelectedLessonChanged(Lesson? value) => LoadSheetCommand.Execute(null);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task LoadSheetAsync()
    {
        Message = ErrorMessage = "";
        if (SelectedLesson is not { } lesson)
        {
            Rows = [];
            CanEdit = false;
            LessonTitle = "Выберите занятие слева";
            ReadOnlyReason = "";
            return;
        }

        var sheet = await attendance.GetSheetAsync(lesson.Id);
        if (SelectedLesson?.Id != lesson.Id)
            return;

        Rows = sheet.Rows.Select(r => new AttendanceRowViewModel(r, sheet.CanChangeStatus)).ToList();
        CanEdit = sheet.CanEdit;
        ReadOnlyReason = sheet.ReadOnlyReason ?? "";
        LessonTitle = $"{lesson.Date:dd.MM.yyyy}, {lesson.PairNumber}-я пара — {lesson.Subject?.Name} ({lesson.Group?.Name}, {lesson.Teacher?.FullName})";
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private async Task SaveAsync()
    {
        if (SelectedLesson is not { } lesson)
            return;

        try
        {
            var result = await attendance.SaveSheetAsync(lesson.Id, Rows.Where(r => r.IsAbsent).Select(r => r.StudentId).ToList());
            await LoadSheetAsync();
            Message = $"Сохранено. Отмечено новых пропусков: {result.Added}, снято отметок: {result.Removed}.";
            if (result.LimitExceeded.Count > 0)
                Message += "\nПревышен месячный лимит неуважительных пропусков: " + string.Join("; ", result.LimitExceeded) +
                           ". Куратор получил уведомление.";
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ToggleStatusAsync(AttendanceRowViewModel row)
    {
        if (row.AbsenceId is not { } absenceId)
            return;

        try
        {
            var status = row.Status == AbsenceStatus.Excused ? AbsenceStatus.Unexcused : AbsenceStatus.Excused;
            await attendance.ChangeStatusAsync(absenceId, status);
            await LoadSheetAsync();
            Message = $"{row.Name}: статус пропуска — «{EnumNameConverter.Name(status)}».";
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}

public partial class ExcusesViewModel(IExcuseService excuses, UserSession session, IDialogService dialogs) : ObservableObject, IPageViewModel
{
    public static IReadOnlyList<Option<DocumentType>> Types { get; } =
        Enum.GetValues<DocumentType>().Select(t => new Option<DocumentType>(t, EnumNameConverter.Name(t))).ToList();

    public static IReadOnlyList<Option<DocumentStatus?>> StatusFilters { get; } =
    [
        new(null, "Все"),
        .. Enum.GetValues<DocumentStatus>().Select(s => new Option<DocumentStatus?>(s, EnumNameConverter.Name(s)))
    ];

    public bool CanRegister => session.Role is UserRole.Admin or UserRole.Headman;
    public bool CanReview => session.Role is UserRole.Admin or UserRole.Curator;

    [ObservableProperty] private Option<DocumentStatus?> _statusFilter = StatusFilters[0];
    [ObservableProperty] private IReadOnlyList<ExcuseDocument> _documents = [];
    [ObservableProperty] private IReadOnlyList<Student> _students = [];
    [ObservableProperty] private ExcuseDocument _draft = NewDraft();
    [ObservableProperty] private string _rejectComment = "";
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _errorMessage = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApproveCommand), nameof(RejectCommand), nameof(DeleteCommand))]
    private ExcuseDocument? _selected;

    private static ExcuseDocument NewDraft()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new ExcuseDocument { Type = DocumentType.MedicalCertificate, IssueDate = today, ValidFrom = today, ValidTo = today };
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (CanRegister)
            Students = await excuses.GetStudentsAsync();
        await RefreshAsync();
    }

    partial void OnStatusFilterChanged(Option<DocumentStatus?> value) => RefreshCommand.Execute(null);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task RefreshAsync() => Documents = await excuses.GetDocumentsAsync(StatusFilter.Value);

    [RelayCommand]
    private Task RegisterAsync() => RunAsync(async () =>
    {
        await excuses.RegisterAsync(Draft);
        Draft = NewDraft();
        return "Документ зарегистрирован и передан куратору на проверку.";
    });

    private bool IsPendingSelected() => Selected?.Status == DocumentStatus.Pending;

    [RelayCommand(CanExecute = nameof(IsPendingSelected))]
    private Task ApproveAsync() => RunAsync(async () =>
    {
        var excused = await excuses.ApproveAsync(Selected!.Id);
        return $"Документ подтверждён. Пропусков признано уважительными: {excused}.";
    });

    [RelayCommand(CanExecute = nameof(IsPendingSelected))]
    private Task RejectAsync() => RunAsync(async () =>
    {
        await excuses.RejectAsync(Selected!.Id, RejectComment);
        RejectComment = "";
        return "Документ отклонён.";
    });

    [RelayCommand(CanExecute = nameof(IsPendingSelected))]
    private Task DeleteAsync() => dialogs.Confirm($"Удалить документ № {Selected!.Number}? Он ещё не проверен куратором.")
        ? RunAsync(async () =>
        {
            await excuses.DeleteAsync(Selected!.Id);
            return "Документ удалён.";
        })
        : Task.CompletedTask;

    private async Task RunAsync(Func<Task<string>> action)
    {
        Message = ErrorMessage = "";
        try
        {
            var result = await action();
            await RefreshAsync();
            Message = result;
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}

public partial class MyAbsencesViewModel(IAttendanceService attendance) : ObservableObject, IPageViewModel
{
    [ObservableProperty] private IReadOnlyList<AbsenceInfo> _items = [];
    [ObservableProperty] private string _summary = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        Items = await attendance.GetMyAbsencesAsync();
        var total = Items.Sum(a => a.Hours);
        var unexcused = Items.Where(a => a.Status == AbsenceStatus.Unexcused).Sum(a => a.Hours);
        Summary = $"Всего пропущено: {total} ч, из них по неуважительной причине: {unexcused} ч.";
    }
}

public partial class NotificationsViewModel(INotificationService notifications) : ObservableObject, IPageViewModel
{
    [ObservableProperty] private IReadOnlyList<Notification> _items = [];

    [RelayCommand]
    private async Task LoadAsync() => Items = await notifications.GetMyAsync();

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        await notifications.MarkAllReadAsync();
        await LoadAsync();
    }
}
