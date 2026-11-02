using Dapper;
using Infrastructure.Observability;
using Infrastructure.Persistence.PostgreSql.Observability;
using Infrastructure.Persistence.PostgreSql.HealthChecks;
using Infrastructure.Persistence.PostgreSql.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using System.Data;

namespace InfrastructureTests;

[NonParallelizable]
public sealed class IntegrationPersistenceTests : BaseRepositoryTests
{
    protected override Task InternalSetup() => Connection.ExecuteAsync("""
        CREATE TABLE customers (id uuid PRIMARY KEY, name text, document text, phone text, email text, status text);
        CREATE TABLE order_status_history (order_id uuid, sequence bigint, previous_status text, new_status text,
            occurred_at timestamptz, reason text);
        """);

    [Test]
    public async Task MissingCustomerIsASuccessfulQuery()
    {
        using var listener = new IntegrationMetricListener();
        using var metrics = new IntegrationMetrics();
        var repository = new CustomerRepository(Connection, metrics: metrics);

        var customer = await repository.GetCustomer(Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(customer, Is.Null);
            Assert.That(listener.Measurements.Single()["outcome"], Is.EqualTo("success"));
        });
    }

    [Test]
    public async Task InvalidPersistedStatusDoesNotTurnTheSuccessfulQueryIntoADatabaseFailure()
    {
        var id = Guid.NewGuid();
        await Connection.ExecuteAsync("INSERT INTO customers VALUES (@id, 'Cliente', '52998224725', '11999999999', 'test@example.invalid', 'Unknown')", new { id });
        using var listener = new IntegrationMetricListener();
        using var metrics = new IntegrationMetrics();
        var repository = new CustomerRepository(Connection, metrics: metrics);

        Assert.ThrowsAsync<DataException>(() => repository.GetCustomer(id));

        Assert.That(listener.Measurements.Single()["outcome"], Is.EqualTo("success"));
    }

    [Test]
    public async Task FailedCommandAndRollbackAreSeparateOperations()
    {
        using var listener = new IntegrationMetricListener();
        using var metrics = new IntegrationMetrics();
        var database = new PostgreSqlOperations(Connection, metrics);
        using var transaction = database.BeginTransaction();

        Assert.ThrowsAsync<PostgresException>(() => database.ExecuteAsync("INSERT INTO missing_table VALUES (1)", transaction: transaction));
        database.Rollback(transaction);
        Assert.That(await Connection.QuerySingleAsync<int>("SELECT 1"), Is.EqualTo(1));

        Assert.That(listener.Measurements.Select(point => (point["operation"], point["outcome"])), Is.EqualTo(new[]
        {
            ((object?)"transaction.begin", (object?)"success"),
            ((object?)"command.execute", (object?)"failure"),
            ((object?)"transaction.rollback", (object?)"success")
        }));
    }

    [Test]
    public async Task CommittedCommandAndTransactionAreCountedIndividually()
    {
        using var listener = new IntegrationMetricListener();
        using var metrics = new IntegrationMetrics();
        var database = new PostgreSqlOperations(Connection, metrics);
        using var transaction = database.BeginTransaction();

        await database.ExecuteAsync("SELECT 1", transaction: transaction);
        database.Commit(transaction);

        Assert.That(listener.Measurements.Select(point => point["operation"]), Is.EqualTo(new[]
            { "transaction.begin", "command.execute", "transaction.commit" }));
        Assert.That(listener.Measurements.All(point => Equals(point["outcome"], "success")), Is.True);
    }

    [Test]
    public async Task HistoryReaderKeepsItsConnectionAndQuerySeparateFromApplicationTraffic()
    {
        using var listener = new IntegrationMetricListener();
        using var metrics = new IntegrationMetrics();
        var reader = new OrderMetricsHistoryReader(PostgresContainer.GetConnectionString(), metrics);

        var history = await reader.ReadAsync(Guid.NewGuid(), 1, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(history, Is.Empty);
            Assert.That(listener.Measurements.Select(point => point["operation"]), Is.EqualTo(new[] { "connection.open", "command.execute" }));
            Assert.That(listener.Measurements.All(point => Equals(point["purpose"], "telemetry")), Is.True);
        });
    }

    [Test]
    public async Task ReadinessConnectionIsNotCountedAsApplicationTraffic()
    {
        using var listener = new IntegrationMetricListener();
        using var metrics = new IntegrationMetrics();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = PostgresContainer.GetConnectionString()
        }).Build();
        var readiness = new PostgreSqlHealthCheck(configuration, metrics);

        var result = await readiness.CheckHealthAsync(new HealthCheckContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
            Assert.That(listener.Measurements.Single()["purpose"], Is.EqualTo("readiness"));
            Assert.That(listener.Measurements.Single()["operation"], Is.EqualTo("connection.open"));
        });
    }
}
