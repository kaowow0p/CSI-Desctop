using System.Windows;
using Attendance.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Desktop.Views;

public partial class MainWindow : Window
{
    private bool _loggingOut;

    public MainWindow(MainViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.LogoutRequested += () =>
        {
            _loggingOut = true;
            Close();
        };
        viewModel.ChangePasswordRequested += () =>
        {
            var dialog = services.GetRequiredService<ChangePasswordWindow>();
            dialog.Owner = this;
            dialog.ShowDialog();
        };
    }

    public event Action? LoggedOut;

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (_loggingOut)
            LoggedOut?.Invoke();
        else
            Application.Current.Shutdown();
    }
}
