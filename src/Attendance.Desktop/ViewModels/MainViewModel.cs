using Attendance.Domain;
using Attendance.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using static Attendance.Domain.UserRole;

namespace Attendance.Desktop.ViewModels;

public sealed record MenuEntry(string Title, UserRole[] Roles, Func<IServiceProvider, object> CreatePage)
{
    public override string ToString() => Title;
}

public partial class MainViewModel : ObservableObject
{
    private static readonly MenuEntry[] Menu =
    [
        new("Журнал посещаемости", [Admin, Curator, Teacher, Headman], Open<JournalViewModel>),
        new("Справки", [Admin, Curator, Headman], Open<ExcusesViewModel>),
        new("Мои пропуски", [Student], Open<MyAbsencesViewModel>),
        new("Уведомления", [Admin, Curator], Open<NotificationsViewModel>),
        new("Отчёты", [Admin, Curator, Teacher, Headman], Open<ReportsViewModel>),
        new("Расписание", [Admin], Open<ScheduleViewModel>),
        new("Группы", [Admin], Open<GroupsViewModel>),
        new("Студенты", [Admin], Open<StudentsViewModel>),
        new("Преподаватели", [Admin], Open<TeachersViewModel>),
        new("Дисциплины", [Admin], Open<SubjectsViewModel>),
        new("Пользователи", [Admin], Open<UsersViewModel>),
        new("Журнал аудита", [Admin], Open<AuditViewModel>),
    ];

    private readonly IAuthService _auth;
    private readonly IServiceProvider _services;

    public MainViewModel(UserSession session, IAuthService auth, IServiceProvider services)
    {
        _auth = auth;
        _services = services;
        UserInfo = $"{session.FullName} · {session.RoleName}";
        MenuItems = Menu.Where(m => m.Roles.Contains(session.Role)).ToList();
        SelectedItem = MenuItems.FirstOrDefault();
    }

    public string UserInfo { get; }
    public IReadOnlyList<MenuEntry> MenuItems { get; }

    [ObservableProperty]
    private MenuEntry? _selectedItem;

    [ObservableProperty]
    private object? _currentPage;

    public event Action? LogoutRequested;
    public event Action? ChangePasswordRequested;

    partial void OnSelectedItemChanged(MenuEntry? value) => CurrentPage = value?.CreatePage(_services);

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        LogoutRequested?.Invoke();
    }

    [RelayCommand]
    private void ChangePassword() => ChangePasswordRequested?.Invoke();

    private static object Open<T>(IServiceProvider services) where T : IPageViewModel
    {
        var page = services.GetRequiredService<T>();
        page.LoadCommand.Execute(null);
        return page;
    }
}
