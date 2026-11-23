using Attendance.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Attendance.Desktop.ViewModels;

public partial class ChangePasswordViewModel(IAuthService auth, UserSession session) : ObservableObject
{
    [ObservableProperty]
    private string _errorMessage = "";

    public string CurrentPassword { private get; set; } = "";
    public string NewPassword { private get; set; } = "";
    public string ConfirmPassword { private get; set; } = "";

    public string Hint => session.MustChangePassword
        ? "Это первый вход — задайте собственный пароль.\n" + PasswordPolicy.Description
        : PasswordPolicy.Description;

    public event Action? PasswordChanged;

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";
        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Новый пароль и подтверждение не совпадают.";
            return;
        }

        try
        {
            await auth.ChangePasswordAsync(CurrentPassword, NewPassword);
            PasswordChanged?.Invoke();
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
