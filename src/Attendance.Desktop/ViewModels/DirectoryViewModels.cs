using System.ComponentModel;
using Attendance.Domain;
using Attendance.Domain.Entities;
using Attendance.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Attendance.Desktop.ViewModels;

public partial class GroupsViewModel(IDirectoryService directory, IUserService users, IDialogService dialogs)
    : CrudViewModel<Group>(dialogs)
{
    [ObservableProperty]
    private IReadOnlyList<LookupItem> _curators = [];

    protected override async Task LoadLookupsAsync() =>
        Curators = [LookupItem.None, .. await AccountsAsync(users, UserRole.Curator)];

    protected override Task<IReadOnlyList<Group>> LoadItemsAsync() => directory.GetGroupsAsync();
    protected override Task<int> SaveItemAsync(Group item) => directory.SaveGroupAsync(item);
    protected override Task DeleteItemAsync(Group item) => directory.DeleteGroupAsync(item.Id);
    protected override int IdOf(Group item) => item.Id;
    protected override Group CreateNew() => new() { Course = 1 };
    protected override string DeleteQuestion(Group item) => $"Удалить группу «{item.Name}»?";

    internal static async Task<IEnumerable<LookupItem>> AccountsAsync(IUserService users, params UserRole[] roles) =>
        (await users.GetUsersAsync())
            .Where(u => roles.Contains(u.RoleId))
            .Select(u => new LookupItem(u.Id, $"{u.FullName} ({u.Login})"));
}

public partial class StudentsViewModel(IDirectoryService directory, IUserService users, IDialogService dialogs)
    : CrudViewModel<Student>(dialogs)
{
    [ObservableProperty]
    private IReadOnlyList<Group> _groups = [];

    [ObservableProperty]
    private IReadOnlyList<LookupItem> _accounts = [];

    protected override async Task LoadLookupsAsync()
    {
        Groups = await directory.GetGroupsAsync();
        Accounts = [LookupItem.None, .. await GroupsViewModel.AccountsAsync(users, UserRole.Student, UserRole.Headman)];
    }

    protected override Task<IReadOnlyList<Student>> LoadItemsAsync() => directory.GetStudentsAsync();
    protected override Task<int> SaveItemAsync(Student item) => directory.SaveStudentAsync(item);
    protected override Task DeleteItemAsync(Student item) => directory.DeleteStudentAsync(item.Id);
    protected override int IdOf(Student item) => item.Id;
    protected override Student CreateNew() => new() { GroupId = Groups.FirstOrDefault()?.Id ?? 0 };
    protected override string DeleteQuestion(Student item) => $"Удалить студента {item.FullName}?";
}

public partial class TeachersViewModel(IDirectoryService directory, IUserService users, IDialogService dialogs)
    : CrudViewModel<Teacher>(dialogs)
{
    [ObservableProperty]
    private IReadOnlyList<LookupItem> _accounts = [];

    protected override async Task LoadLookupsAsync() =>
        Accounts = [LookupItem.None, .. await GroupsViewModel.AccountsAsync(users, UserRole.Teacher)];

    protected override Task<IReadOnlyList<Teacher>> LoadItemsAsync() => directory.GetTeachersAsync();
    protected override Task<int> SaveItemAsync(Teacher item) => directory.SaveTeacherAsync(item);
    protected override Task DeleteItemAsync(Teacher item) => directory.DeleteTeacherAsync(item.Id);
    protected override int IdOf(Teacher item) => item.Id;
    protected override string DeleteQuestion(Teacher item) => $"Удалить преподавателя {item.FullName}?";
}

public partial class SubjectsViewModel(IDirectoryService directory, IDialogService dialogs)
    : CrudViewModel<Subject>(dialogs)
{
    protected override Task<IReadOnlyList<Subject>> LoadItemsAsync() => directory.GetSubjectsAsync();
    protected override Task<int> SaveItemAsync(Subject item) => directory.SaveSubjectAsync(item);
    protected override Task DeleteItemAsync(Subject item) => directory.DeleteSubjectAsync(item.Id);
    protected override int IdOf(Subject item) => item.Id;
    protected override string DeleteQuestion(Subject item) => $"Удалить дисциплину «{item.Name}»?";
}

public partial class UsersViewModel(IUserService users, IDialogService dialogs) : CrudViewModel<User>(dialogs)
{
    [ObservableProperty]
    private IReadOnlyList<Role> _roles = [];

    /// <summary>Временный пароль: обязателен для нового пользователя, для существующего — сброс пароля.</summary>
    [ObservableProperty]
    private string _newPassword = "";

    public string PasswordHint => PasswordPolicy.Description + " Пользователь сменит его при первом входе.";

    protected override async Task LoadLookupsAsync() => Roles = await users.GetRolesAsync();
    protected override Task<IReadOnlyList<User>> LoadItemsAsync() => users.GetUsersAsync();
    protected override Task DeleteItemAsync(User item) => users.DeleteUserAsync(item.Id);
    protected override int IdOf(User item) => item.Id;
    protected override User CreateNew() => new() { RoleId = UserRole.Student };

    protected override string DeleteQuestion(User item) =>
        $"Удалить пользователя {item.Login}?\nЕсли он уже вносил данные, удаление будет невозможно — заблокируйте его.";

    protected override async Task<int> SaveItemAsync(User item)
    {
        var id = await users.SaveUserAsync(item, NewPassword);
        NewPassword = "";
        return id;
    }

    // Пароль, введённый для одного пользователя, не должен применяться к другому.
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(Edit))
            NewPassword = "";
    }
}
