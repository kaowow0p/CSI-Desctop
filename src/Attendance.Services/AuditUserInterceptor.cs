using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Attendance.Services;

/// <summary>
/// При каждом открытии подключения записывает id вошедшего пользователя в настройку сеанса PostgreSQL app.user_id.
/// Её читают триггеры аудита (database/04_audit.sql), чтобы знать, кто изменил данные.
/// </summary>
internal sealed class AuditUserInterceptor(UserSession session) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.user_id', @user_id, false)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "user_id";
        parameter.Value = session.IsAuthenticated ? session.UserId.ToString() : ""; // до входа — пустая строка, в журнале NULL
        command.Parameters.Add(parameter);
        return command;
    }
}
