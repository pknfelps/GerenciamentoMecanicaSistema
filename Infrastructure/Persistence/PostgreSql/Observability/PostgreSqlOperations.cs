using Dapper;
using Infrastructure.Observability;
using System.Data;

namespace Infrastructure.Persistence.PostgreSql.Observability;

public sealed class PostgreSqlOperations(IDbConnection connection, IntegrationMetrics? metrics = null, string purpose = "application")
{
    public Task<int> ExecuteAsync(string sql, object? parameters = null, IDbTransaction? transaction = null) =>
        IntegrationMetrics.MeasureAsync(metrics, "postgresql", "command.execute", purpose,
            () => connection.ExecuteAsync(sql, parameters, transaction));

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null) =>
        IntegrationMetrics.MeasureAsync(metrics, "postgresql", "command.execute", purpose,
            () => connection.QueryAsync<T>(sql, parameters, transaction));

    public Task<IEnumerable<T>> QueryAsync<T>(CommandDefinition command, CancellationToken callerCancellation) =>
        IntegrationMetrics.MeasureAsync(metrics, "postgresql", "command.execute", purpose,
            () => connection.QueryAsync<T>(command), callerCancellation);

    public Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, IDbTransaction? transaction = null) =>
        IntegrationMetrics.MeasureAsync(metrics, "postgresql", "command.execute", purpose,
            () => connection.QuerySingleOrDefaultAsync<T>(sql, parameters, transaction));

    public IDbTransaction BeginTransaction() => IntegrationMetrics.Measure(metrics, "postgresql", "transaction.begin", purpose,
        connection.BeginTransaction);

    public void Commit(IDbTransaction transaction) => IntegrationMetrics.Measure(metrics, "postgresql", "transaction.commit", purpose,
        () => { transaction.Commit(); return true; });

    public void Rollback(IDbTransaction transaction) => IntegrationMetrics.Measure(metrics, "postgresql", "transaction.rollback", purpose,
        () => { transaction.Rollback(); return true; });
}
