using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Attendance.Services;

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
        parameter.Value = session.IsAuthenticated ? session.UserId.ToString() : "";
        command.Parameters.Add(parameter);
        return command;
    }
}
