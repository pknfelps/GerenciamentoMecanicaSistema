using Dapper;
using Npgsql;
using Service.Interface.Observability;

namespace Infrastructure.Persistence.PostgreSql.Observability;

public sealed class OrderMetricsHistoryReader(string connectionString) : IOrderMetricsHistoryReader
{
    public async Task<IReadOnlyList<OrderHistoryEntry>> ReadAsync(Guid orderId, int timeoutSeconds, CancellationToken cancellationToken)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString)
        { Timeout = timeoutSeconds, CommandTimeout = timeoutSeconds, CancellationTimeout = -1 };
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        var entries = await connection.QueryAsync<OrderHistoryEntry>(new CommandDefinition(
            """
            SELECT sequence, previous_status AS PreviousStatus, new_status AS NewStatus,
                occurred_at AS OccurredAt, reason
            FROM order_status_history
            WHERE order_id = @OrderId
            ORDER BY sequence;
            """, new { OrderId = orderId }, commandTimeout: timeoutSeconds, cancellationToken: cancellationToken));
        return entries.ToArray();
    }
}
