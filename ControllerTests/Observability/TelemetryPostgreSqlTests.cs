using Dapper;
using Domain.Interface.Order;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NSubstitute;
using System.Diagnostics;
using System.Net;
using Testcontainers.PostgreSql;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class TelemetryPostgreSqlTests : BaseControllerTests
{
    private PostgreSqlContainer container = null!;
    private TelemetryWebApplicationFactory Telemetry => (TelemetryWebApplicationFactory)TestWebAppFactory;
    private const string PrivateMarker = "private-customer-secret-417.384.220-11";

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        container = new PostgreSqlBuilder("postgres:16.15")
            .WithDatabase("telemetry_tests").WithUsername("postgres").WithPassword("local-tests-only").Build();
        await container.StartAsync();
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.ExecuteAsync("""
            CREATE TABLE customers (
                id UUID PRIMARY KEY, name VARCHAR(100) NOT NULL, document VARCHAR(100) NOT NULL,
                phone VARCHAR(100) NOT NULL, email VARCHAR(100) NOT NULL, status VARCHAR(8) NOT NULL);
            INSERT INTO customers VALUES ('9484ab05-76a8-46e3-b54b-0689df13412e', 'Private Name',
                '662.119.730-63', '11912345678', 'private@example.invalid', 'Active');
            """);
    }

    [OneTimeTearDown]
    public async Task RemoveDatabase() => await container.DisposeAsync();

    protected override TestWebApplicationFactory CreateFactory() => new TelemetryWebApplicationFactory(container.GetConnectionString(), true)
    { TelemetryEnabled = "true", TelemetryEndpoint = "http://127.0.0.1:1" };
    protected override void MockService() { }

    [Test]
    public async Task RealRepositoryProducesChildSpanWithoutSqlParametersOrConnectionIdentity()
    {
        using var response = await TestClient.GetAsync("/Customers?document=66211973063");
        Telemetry.Flush();
        var http = Telemetry.Spans.Single(x => x.Kind == ActivityKind.Server);
        var database = Telemetry.Spans.Single(x => x.Source.Name == "Npgsql");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(database.ParentSpanId, Is.EqualTo(http.SpanId));
            Assert.That(database.TraceId, Is.EqualTo(http.TraceId));
            Assert.That(database.DisplayName, Is.EqualTo("postgresql"));
            Assert.That(database.GetTagItem("db.system.name"), Is.EqualTo("postgresql"));
            Assert.That(database.TagObjects.Select(x => x.Key), Is.SubsetOf(new[]
            { "db.system.name", "db.namespace", "db.operation.name", "db.response.status_code", "error.type" }));
        });
        AssertNoSensitiveData();
    }

    [Test]
    public async Task RealPostgreSqlErrorExportsTypeAndFixedMessageOnly()
    {
        TestWebAppFactory.OrderServiceMock.GetOrderStatus(Arg.Any<Guid>()).Returns(async _ =>
        {
            await using var connection = new NpgsqlConnection(container.GetConnectionString());
            await connection.ExecuteAsync($"DO $$ BEGIN RAISE EXCEPTION '{PrivateMarker}'; END $$;");
            return WorkOrderStatus.Received;
        });
        using var response = await TestClient.GetAsync($"/Orders/{Guid.NewGuid()}/status");
        Telemetry.Flush();
        var database = Telemetry.Spans.Single(x => x.Source.Name == "Npgsql" && x.Status == ActivityStatusCode.Error);
        var error = database.Events.Single(x => x.Name == "exception");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(database.StatusDescription, Is.EqualTo("Falha na operação PostgreSQL."));
            Assert.That(database.GetTagItem("db.response.status_code"), Is.EqualTo("P0001"));
            Assert.That(error.Tags.Single(x => x.Key == "exception.type").Value, Is.EqualTo(typeof(PostgresException).FullName));
            Assert.That(error.Tags.Single(x => x.Key == "exception.message").Value, Is.EqualTo("Falha na operação PostgreSQL."));
            Assert.That(error.Tags.Any(x => x.Key == "exception.stacktrace"), Is.False);
        });
        AssertNoSensitiveData();
    }

    [TestCase("live")]
    [TestCase("ready")]
    [TestCase("startup")]
    [TestCase("READY")]
    [TestCase("ready/")]
    public async Task ProbesKeepMetricsAndHealthWithoutHttpOrDatabaseSpans(string probe)
    {
        using var response = await TestClient.GetAsync("/health/" + probe);
        Telemetry.Flush();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(Telemetry.Spans, Is.Empty);
            Assert.That(Telemetry.Metrics.Any(x => x.Name == "http.server.request.duration"), Is.True);
        });
    }

    [Test]
    public async Task CollectorUnavailableDoesNotChangePersistenceHealthOrResponses()
    {
        using var first = await TestClient.GetAsync("/Customers");
        Telemetry.Flush();
        using var second = await TestClient.GetAsync("/Customers");
        using var ready = await TestClient.GetAsync("/health/ready");
        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(ready.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
        var health = await TestWebAppFactory.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();
        Assert.That(health.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    private void AssertNoSensitiveData()
    {
        var exported = string.Join(' ', Telemetry.Spans.Select(x => x.DisplayName + x.StatusDescription
            + string.Join(' ', x.TagObjects) + string.Join(' ', x.Events.SelectMany(e => e.Tags))));
        var logged = string.Join(' ', Telemetry.Logs.Select(x => x.Body + x.FormattedMessage
            + string.Join(' ', x.Attributes ?? [])));
        foreach (var marker in new[] { PrivateMarker, "662.119.730-63", "66211973063", "private@example.invalid", "Private Name", "local-tests-only", "SELECT", "RAISE EXCEPTION" })
        {
            Assert.That(exported.Contains(marker, StringComparison.Ordinal), Is.False, "Sensitive trace data must not be exported.");
            Assert.That(logged.Contains(marker, StringComparison.Ordinal), Is.False, "Sensitive log data must not be exported.");
        }
    }
}
