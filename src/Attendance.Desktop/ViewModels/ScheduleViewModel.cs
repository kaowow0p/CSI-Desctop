using System.Collections.ObjectModel;
using System.Globalization;
using Attendance.Domain.Entities;
using Attendance.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Attendance.Desktop.ViewModels;

public sealed record TemplateRow(DayOfWeek Day, short PairNumber, int SubjectId, string SubjectName, int TeacherId, string TeacherName, short Hours)
{
    public string DayName => ScheduleViewModel.DayName(Day);
}

public partial class ScheduleViewModel(IScheduleService schedule, IDirectoryService directory, IDialogService dialogs)
    : CrudViewModel<Lesson>(dialogs)
{
    private static readonly DateTime ThisMonday = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));

    public static IReadOnlyList<LookupItem> Days { get; } =
        Enumerable.Range(1, 6).Select(d => new LookupItem(d, DayName((DayOfWeek)d))).ToList();

    [ObservableProperty] private IReadOnlyList<Group> _groups = [];
    [ObservableProperty] private IReadOnlyList<Subject> _subjects = [];
    [ObservableProperty] private IReadOnlyList<Teacher> _teachers = [];

    [ObservableProperty] private int _groupId;
    [ObservableProperty] private DateTime _from = ThisMonday;
    [ObservableProperty] private DateTime _to = ThisMonday.AddDays(13);

    public ObservableCollection<TemplateRow> TemplateRows { get; } = [];
    [ObservableProperty] private int? _templateDay = 1;
    [ObservableProperty] private short _templatePair = 1;
    [ObservableProperty] private int? _templateSubjectId;
    [ObservableProperty] private int? _templateTeacherId;
    [ObservableProperty] private short _templateHours = 2;
    [ObservableProperty] private DateTime _generateFrom = ThisMonday.AddDays(14);
    [ObservableProperty] private DateTime _generateTo = ThisMonday.AddDays(14 + 27);
    [ObservableProperty] private string _templateError = "";
    [ObservableProperty] private string _templateStatus = "";

    public static string DayName(DayOfWeek day) =>
        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(day));

    protected override async Task LoadLookupsAsync()
    {
        Groups = await directory.GetGroupsAsync();
        Subjects = await directory.GetSubjectsAsync();
        Teachers = await directory.GetTeachersAsync();
        if (GroupId == 0)
            GroupId = Groups.FirstOrDefault()?.Id ?? 0;
    }

    protected override async Task<IReadOnlyList<Lesson>> LoadItemsAsync() =>
        GroupId == 0 ? [] : await schedule.GetLessonsAsync(GroupId, DateOnly.FromDateTime(From), DateOnly.FromDateTime(To));

    protected override Task<int> SaveItemAsync(Lesson item)
    {
        item.GroupId = GroupId;
        return schedule.SaveLessonAsync(item);
    }

    protected override Task DeleteItemAsync(Lesson item) => schedule.DeleteLessonAsync(item.Id);
    protected override int IdOf(Lesson item) => item.Id;
    protected override Lesson CreateNew() => new() { Date = DateOnly.FromDateTime(From), PairNumber = 1, GroupId = GroupId };

    protected override string DeleteQuestion(Lesson item) =>
        $"Удалить занятие {item.Date:dd.MM.yyyy}, {item.PairNumber}-я пара?\nОтметки о пропусках на этом занятии тоже будут удалены.";

    [RelayCommand]
    private Task ShowAsync() => ReloadAsync(null);

    [RelayCommand]
    private void AddTemplateRow()
    {
        TemplateError = TemplateStatus = "";
        var subject = Subjects.FirstOrDefault(s => s.Id == TemplateSubjectId);
        var teacher = Teachers.FirstOrDefault(t => t.Id == TemplateTeacherId);
        if (TemplateDay is not { } day || subject is null || teacher is null)
        {
            TemplateError = "Выберите день, дисциплину и преподавателя.";
            return;
        }

        SetTemplate(TemplateRows.Where(r => r.Day != (DayOfWeek)day || r.PairNumber != TemplatePair)
            .Append(new TemplateRow((DayOfWeek)day, TemplatePair, subject.Id, subject.Name, teacher.Id, teacher.FullName, TemplateHours)));
    }

    [RelayCommand]
    private void RemoveTemplateRow(TemplateRow row) => TemplateRows.Remove(row);

    [RelayCommand]
    private void FillTemplateFromShownWeek()
    {
        TemplateError = TemplateStatus = "";
        var weekStart = DateOnly.FromDateTime(From);
        var week = Items.Where(l => l.Date >= weekStart && l.Date < weekStart.AddDays(7)).ToList();
        if (week.Count == 0)
        {
            TemplateError = "В первой неделе показанного периода нет занятий.";
            return;
        }

        SetTemplate(week.Select(l => new TemplateRow(l.Date.DayOfWeek, l.PairNumber, l.SubjectId, l.Subject?.Name ?? "",
            l.TeacherId, l.Teacher?.FullName ?? "", l.Hours)));
        TemplateStatus = $"Шаблон заполнен по неделе с {weekStart:dd.MM.yyyy}: {week.Count} занятий.";
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        TemplateError = TemplateStatus = "";
        try
        {
            var template = TemplateRows
                .Select(r => new ScheduleTemplateItem(r.Day, r.PairNumber, r.SubjectId, r.TeacherId, r.Hours))
                .ToList();
            var result = await schedule.GenerateAsync(GroupId, DateOnly.FromDateTime(GenerateFrom), DateOnly.FromDateTime(GenerateTo), template);
            TemplateStatus = $"Создано занятий: {result.Created}. Пропущено (пара уже занята): {result.Skipped}.";
            await ReloadAsync(null);
        }
        catch (ServiceException ex)
        {
            TemplateError = ex.Message;
        }
    }

    private void SetTemplate(IEnumerable<TemplateRow> rows)
    {
        var sorted = rows.OrderBy(r => ((int)r.Day + 6) % 7).ThenBy(r => r.PairNumber).ToList();
        TemplateRows.Clear();
        foreach (var row in sorted)
            TemplateRows.Add(row);
    }
}
