using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Attendance.Data;
using Attendance.Desktop.ViewModels;
using Attendance.Desktop.Views;
using Attendance.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Attendance.Desktop;

public partial class App : Application
{
    private readonly IHost _host = CreateHostBuilder([]).Build();

    // Этот метод также находят инструменты EF Core (dotnet ef) при работе с миграциями.
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                services.AddAttendanceData(context.Configuration.GetConnectionString("Attendance"));
                services.AddAttendanceServices();
                services.Configure<AttendanceOptions>(context.Configuration.GetSection("Attendance"));

                services.AddTransient<LoginViewModel>().AddTransient<LoginWindow>();
                services.AddTransient<ChangePasswordViewModel>().AddTransient<ChangePasswordWindow>();
                services.AddTransient<MainViewModel>().AddTransient<MainWindow>();

                services.AddSingleton<IDialogService, DialogService>();
                services.AddTransient<GroupsViewModel>();
                services.AddTransient<StudentsViewModel>();
                services.AddTransient<TeachersViewModel>();
                services.AddTransient<SubjectsViewModel>();
                services.AddTransient<UsersViewModel>();
                services.AddTransient<ScheduleViewModel>();
                services.AddTransient<JournalViewModel>();
                services.AddTransient<ExcusesViewModel>();
                services.AddTransient<MyAbsencesViewModel>();
                services.AddTransient<NotificationsViewModel>();
                services.AddTransient<ReportsViewModel>();
                services.AddTransient<AuditViewModel>();
            });

    public App()
    {
        // Даты и дни недели — по-русски независимо от настроек Windows.
        // Задаётся здесь, а не в async OnStartup: изменение культуры внутри async-метода не сохраняется после await.
        var culture = CultureInfo.GetCultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;
        await _host.StartAsync();
        ShowLogin();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host.Dispose();
        base.OnExit(e);
    }

    /// <summary>Окно входа; после успешного входа — главное окно. Вызывается повторно при выходе из учётной записи.</summary>
    private void ShowLogin()
    {
        var login = _host.Services.GetRequiredService<LoginWindow>();
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        var main = _host.Services.GetRequiredService<MainWindow>();
        main.LoggedOut += ShowLogin;
        MainWindow = main;
        main.Show();
    }

    // Неожиданные ошибки (например, недоступен сервер БД) показываем пользователю вместо аварийного завершения.
    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.GetBaseException().Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
