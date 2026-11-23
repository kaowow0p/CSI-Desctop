using System.Windows;
using Attendance.Desktop.ViewModels;

namespace Attendance.Desktop.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly ChangePasswordViewModel _viewModel;

    public ChangePasswordWindow(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        _viewModel.PasswordChanged += () =>
        {
            MessageBox.Show(this, "Пароль успешно изменён.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        };
        Loaded += (_, _) => CurrentBox.Focus();
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.CurrentPassword = CurrentBox.Password;
        _viewModel.NewPassword = NewBox.Password;
        _viewModel.ConfirmPassword = ConfirmBox.Password;
    }
}
