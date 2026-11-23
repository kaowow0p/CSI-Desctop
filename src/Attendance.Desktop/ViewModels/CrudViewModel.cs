using System.Reflection;
using Attendance.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Attendance.Desktop.ViewModels;

/// <summary>Раздел, который загружает данные при открытии.</summary>
public interface IPageViewModel
{
    IAsyncRelayCommand LoadCommand { get; }
}

/// <summary>Элемент выпадающего списка, допускающего «пустое» значение.</summary>
public sealed record LookupItem(int? Id, string Name)
{
    public static LookupItem None { get; } = new(null, "— не выбрано —");
}

/// <summary>
/// Общая логика страниц справочников: список слева, форма редактирования справа.
/// Форма редактирует копию выбранной записи, поэтому несохранённые изменения не попадают в список.
/// </summary>
public abstract partial class CrudViewModel<T>(IDialogService dialogs) : ObservableObject, IPageViewModel where T : class, new()
{
    private static readonly MethodInfo CloneMethod =
        typeof(object).GetMethod(nameof(MemberwiseClone), BindingFlags.Instance | BindingFlags.NonPublic)!;

    [ObservableProperty]
    private IReadOnlyList<T> _items = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private T? _selected;

    [ObservableProperty]
    private T _edit = new();

    [ObservableProperty]
    private string _errorMessage = "";

    protected IDialogService Dialogs => dialogs;

    protected abstract Task<IReadOnlyList<T>> LoadItemsAsync();
    protected abstract Task<int> SaveItemAsync(T item);
    protected abstract Task DeleteItemAsync(T item);
    protected abstract int IdOf(T item);
    protected virtual Task LoadLookupsAsync() => Task.CompletedTask;
    protected virtual T CreateNew() => new();
    protected virtual string DeleteQuestion(T item) => "Удалить выбранную запись?";

    partial void OnSelectedChanged(T? value)
    {
        Edit = value is null ? CreateNew() : (T)CloneMethod.Invoke(value, null)!;
        ErrorMessage = "";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await LoadLookupsAsync();
        await ReloadAsync(null);
    }

    protected async Task ReloadAsync(int? selectId)
    {
        Items = await LoadItemsAsync();
        Selected = Items.FirstOrDefault(i => IdOf(i) == selectId);
        if (Selected is null)
            Edit = CreateNew();
    }

    [RelayCommand]
    private void New()
    {
        Selected = null;
        Edit = CreateNew();
        ErrorMessage = "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";
        try
        {
            var id = await SaveItemAsync(Edit);
            await ReloadAsync(id);
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private bool CanDelete() => Selected is not null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (Selected is not { } item || !dialogs.Confirm(DeleteQuestion(item)))
            return;

        ErrorMessage = "";
        try
        {
            await DeleteItemAsync(item);
            await ReloadAsync(null);
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
