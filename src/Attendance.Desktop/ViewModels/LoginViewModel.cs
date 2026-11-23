using Attendance.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Attendance.Desktop.ViewModels;

public partial class LoginViewModel(IAuthService auth, UserSession session) : ObservableObject
{
    [ObservableProperty]
    private string _login = "";

    [ObservableProperty]
    private string _errorMessage = "";

    // PasswordBox не поддерживает привязку, значение передаётся из code-behind.
    public string Password { private get; set; } = "";

    public bool MustChangePassword => session.MustChangePassword;

    public event Action? LoggedIn;

    [RelayCommand]
    private async Task SignInAsync()
    {
        ErrorMessage = "";
        try
        {
            await auth.LoginAsync(Login, Password);
            LoggedIn?.Invoke();
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public void CancelLogin() => auth.Logout();
}
