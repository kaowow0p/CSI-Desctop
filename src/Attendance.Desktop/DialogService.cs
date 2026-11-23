using System.Windows;

namespace Attendance.Desktop;

/// <summary>Диалоги для ViewModel, чтобы они не обращались к MessageBox напрямую.</summary>
public interface IDialogService
{
    bool Confirm(string message);
    void Info(string message);

    /// <summary>Окно «Сохранить как»; null — пользователь отменил.</summary>
    string? SaveFile(string defaultName, string filter);
}

public sealed class DialogService : IDialogService
{
    public bool Confirm(string message) =>
        MessageBox.Show(Application.Current.MainWindow, message, "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

    public void Info(string message) =>
        MessageBox.Show(Application.Current.MainWindow, message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

    public string? SaveFile(string defaultName, string filter)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { FileName = defaultName, Filter = filter };
        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
    }
}
