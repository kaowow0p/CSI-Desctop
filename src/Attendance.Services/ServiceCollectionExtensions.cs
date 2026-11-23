using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAttendanceServices(this IServiceCollection services)
    {
        services.AddSingleton<UserSession>();
        services.AddOptions<AttendanceOptions>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IDirectoryService, DirectoryService>();
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IScheduleService, ScheduleService>();
        services.AddTransient<IAttendanceService, AttendanceService>();
        services.AddTransient<IExcuseService, ExcuseService>();
        services.AddTransient<INotificationService, NotificationService>();
        services.AddTransient<IReportService, ReportService>();
        services.AddTransient<IAuditService, AuditService>();
        services.AddSingleton<IInterceptor, AuditUserInterceptor>();
        return services;
    }
}
