using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Attendance.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddAttendanceData(this IServiceCollection services, string? connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Фабрика контекстов: окна WPF живут долго, поэтому контекст создаётся на одну операцию.
        // Перехватчики из DI (передача текущего пользователя в триггеры аудита) подключаются к каждому контексту.
        services.AddDbContextFactory<AttendanceDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetServices<IInterceptor>()));
        return services;
    }
}
