using Dapper;
using Infrastructure.Persistence.PostgreSql.Observability;
using Npgsql;
using Testcontainers.PostgreSql;

namespace InfrastructureTests;

[NonParallelizable]
public sealed class OrderMetricsHistoryReaderTests
{
    private PostgreSqlContainer container = null!;
    private NpgsqlConnection connection = null!;
    private OrderMetricsHistoryReader reader = null!;
    private readonly Guid orderId = Guid.NewGuid();

    [OneTimeSetUp]
    public async Task StartDatabase()
    {
        container = new PostgreSqlBuilder("postgres:16.15").WithDatabase("business_history_tests")
            .WithUsername("postgres").WithPassword("local-tests-only").Build();
        await container.StartAsync();
        connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await connection.ExecuteAsync("""
            CREATE TABLE order_status_history (order_id UUID, sequence INTEGER, previous_status TEXT,
                new_status TEXT, occurred_at TIMESTAMPTZ, reason TEXT);
            INSERT INTO order_status_history VALUES (@Id, 2, 'Received', 'InDiagnosis', '2026-10-14T04:00:00Z', NULL);
            INSERT INTO order_status_history VALUES (@Id, 1, NULL, 'Received', '2026-10-14T03:00:00Z', NULL);
            INSERT INTO order_status_history VALUES (@Other, 1, NULL, 'Received', '2026-10-14T03:00:00Z', NULL);
            """, new { Id = orderId, Other = Guid.NewGuid() });
        var settings = new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { ApplicationName = "business-reader-test", Pooling = false };
        reader = new OrderMetricsHistoryReader(settings.ConnectionString);
    }

    [OneTimeTearDown]
    public async Task StopDatabase()
    {
        if (connection is not null) await connection.DisposeAsync();
        if (container is not null) await container.DisposeAsync();
    }

    [Test]
    public async Task ReadsOnlyTheRequestedOrdersRetainedHistoryInSequenceOrderAndDisposesConnection()
    {
        var history = await reader.ReadAsync(orderId, 1, CancellationToken.None);
        Assert.That(history.Select(entry => entry.Sequence), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(history[0].PreviousStatus, Is.Null);
        Assert.That(history[1].NewStatus, Is.EqualTo("InDiagnosis"));
        Assert.That(history.All(entry => entry.OccurredAt.Kind == DateTimeKind.Utc), Is.True);
        Assert.That(await reader.ReadAsync(Guid.NewGuid(), 1, CancellationToken.None), Is.Empty);
        await AssertReaderDisconnected();
    }

    [Test]
    public async Task CancellationInterruptsBlockedReadAndDisposesConnection()
    {
        await using var transaction = await connection.BeginTransactionAsync();
        await connection.ExecuteAsync("LOCK TABLE order_status_history IN ACCESS EXCLUSIVE MODE;", transaction: transaction);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        try
        {
            Assert.That(async () => await reader.ReadAsync(orderId, 1, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
        }
        finally { await transaction.RollbackAsync(); }
        await AssertReaderDisconnected();
    }

    [Test]
    public async Task SqlFailureDisposesConnection()
    {
        await connection.ExecuteAsync("ALTER TABLE order_status_history RENAME TO unavailable_history;");
        try { Assert.ThrowsAsync<PostgresException>(async () => await reader.ReadAsync(orderId, 1, CancellationToken.None)); }
        finally { await connection.ExecuteAsync("ALTER TABLE unavailable_history RENAME TO order_status_history;"); }
        await AssertReaderDisconnected();
    }

    private async Task AssertReaderDisconnected()
    {
        // pg_stat_activity is read in a fresh statement after each short-lived reader has been disposed.
        Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*)::int FROM pg_stat_activity WHERE application_name = 'business-reader-test'"), Is.Zero);
    }
}
