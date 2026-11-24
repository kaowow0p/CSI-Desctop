using System.Windows;
using System.Windows.Controls;
using Attendance.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private readonly IServiceProvider _services;

    public LoginWindow(LoginViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        _services = services;
        _viewModel.LoggedIn += OnLoggedIn;
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = ((PasswordBox)sender).Password;

    private void OnLoggedIn()
    {
        if (_viewModel.MustChangePassword)
        {
            var dialog = _services.GetRequiredService<ChangePasswordWindow>();
            dialog.Owner = this;
            if (dialog.ShowDialog() != true)
            {
                _viewModel.CancelLogin();
                return;
            }
        }

        DialogResult = true;
    }
}
