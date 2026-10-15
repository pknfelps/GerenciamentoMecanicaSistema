using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Domain.Interface.Order;
using Dapper;
using Npgsql;
using NSubstitute;
using System.Net;
using System.Text.RegularExpressions;
using Testcontainers.PostgreSql;
using System.Net.Http.Json;
using Service.Interface.Observability;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class CollectorExportTests : BaseControllerTests
{
    private IContainer collector = null!;
    private PostgreSqlContainer database = null!;
    private TelemetryWebApplicationFactory Telemetry => (TelemetryWebApplicationFactory)TestWebAppFactory;

    [OneTimeSetUp]
    public async Task StartCollector()
    {
        FileInfo configuration;
        var infrastructureConfiguration = TestContext.Parameters.Exists("CollectorConfigPath");
        if (infrastructureConfiguration)
        {
            var path = TestContext.Parameters.Get("CollectorConfigPath");
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Collector configuration was not found.");
            configuration = new FileInfo(path);
            if (!configuration.Exists)
                throw new InvalidOperationException("Collector configuration was not found.");
        }
        else
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "observability", "collector-local.yaml")))
                directory = directory.Parent;
            if (directory is null) throw new InvalidOperationException("Collector configuration was not found.");
            configuration = new FileInfo(Path.Combine(directory.FullName, "observability", "collector-local.yaml"));
        }
        database = new PostgreSqlBuilder("postgres:16.15")
            .WithDatabase("collector_tests").WithUsername("postgres").WithPassword("private-local-password").Build();
        await database.StartAsync();
        var builder = new ContainerBuilder("otel/opentelemetry-collector-contrib:0.161.0")
            .WithPortBinding(4318, true);
        if (infrastructureConfiguration)
        {
            builder = builder
                .WithBindMount(configuration.FullName, $"/etc/otelcol-contrib/{configuration.Name}",
                    DotNet.Testcontainers.Configurations.AccessMode.ReadOnly)
                .WithCommand($"--config=/etc/otelcol-contrib/{configuration.Name}",
                    "--config=yaml:exporters::debug::verbosity: detailed")
                .WithEnvironment("GOMEMLIMIT", "192MiB")
                .WithCreateParameterModifier(parameters =>
                {
                    parameters.User = "10001:10001";
                    var host = parameters.HostConfig
                        ?? throw new InvalidOperationException("Docker host configuration was not created.");
                    host.ReadonlyRootfs = true;
                    host.CapDrop = ["ALL"];
                    host.SecurityOpt = ["no-new-privileges:true"];
                    host.Memory = 256 * 1024 * 1024;
                    host.NanoCPUs = 500_000_000;
                })
                .WithPortBinding(13133, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request =>
                    request.ForPort(13133).ForPath("/").ForStatusCode(HttpStatusCode.OK)));
        }
        else
        {
            builder = builder
                .WithResourceMapping(configuration, "/etc/otelcol-contrib")
                .WithCommand($"--config=/etc/otelcol-contrib/{configuration.Name}")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request =>
                    request.ForPort(4318).ForPath("/").ForStatusCode(HttpStatusCode.NotFound)));
        }
        collector = builder.Build();
        await collector.StartAsync();
    }

    [OneTimeTearDown]
    public async Task RemoveCollector()
    {
        if (collector is not null)
            await collector.DisposeAsync();
        if (database is not null)
            await database.DisposeAsync();
    }

    protected override TestWebApplicationFactory CreateFactory() => new TelemetryWebApplicationFactory(database.GetConnectionString())
    {
        TelemetryEnabled = "true",
        TelemetryEndpoint = $"http://{collector.Hostname}:{collector.GetMappedPublicPort(4318)}"
    };
    protected override void MockService() { }

    [Test]
    public async Task ExportsAllSignalsOverOtlpAndContinuesWhenCollectorStops()
    {
        var infrastructureConfiguration = TestContext.Parameters.Exists("CollectorConfigPath");
        await TestContext.Out.WriteLineAsync(infrastructureConfiguration
            ? "Collector configuration: infrastructure; read-only, UID/GID 10001, 256 MiB, 0.5 CPU."
            : "Collector configuration: local.");
        if (infrastructureConfiguration)
        {
            using var healthClient = new HttpClient();
            using var health = await healthClient.GetAsync($"http://{collector.Hostname}:{collector.GetMappedPublicPort(13133)}/");
            Assert.That(health.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
        var logger = TestWebAppFactory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Service.CollectorValidation");
        using (logger.BeginScope(new Dictionary<string, object?>
        { ["RequestPath"] = "/private-person", ["Authorization"] = "Bearer private-token" }))
            logger.LogInformation("Validação local da exportação.");
        using var first = await TestClient.GetAsync("/Customers?document=private-query");
        Telemetry.Flush();
        var traceId = Telemetry.Spans.Single(x => x.Kind == System.Diagnostics.ActivityKind.Server).TraceId.ToString();
        using var creation = await TestClient.PostAsJsonAsync("/Orders", new
        { customerId = Guid.NewGuid(), vehicleId = Guid.NewGuid(), services = Array.Empty<object>(), materials = Array.Empty<object>() });
        TestWebAppFactory.Services.GetRequiredService<IOrderNotificationMetrics>().RecordNotification(true, true);
        var completedOrderId = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(database.GetConnectionString()))
        {
            await connection.ExecuteAsync("""
                CREATE TABLE order_status_history (order_id UUID, sequence INTEGER, previous_status TEXT,
                    new_status TEXT, occurred_at TIMESTAMPTZ, reason TEXT);
                """);
            await connection.ExecuteAsync("""
                INSERT INTO order_status_history VALUES (@OrderId, @Sequence, @PreviousStatus, @NewStatus, @OccurredAt, @Reason);
                """, OrderDurationMeasurementsTests.History().Select(entry => new
                {
                    OrderId = completedOrderId, entry.Sequence, entry.PreviousStatus, entry.NewStatus, entry.OccurredAt, entry.Reason
                }));
        }
        using var completion = await TestClient.PatchAsync($"/Orders/{completedOrderId}/execution/complete", null);
        Assert.That(completion.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var id = Guid.NewGuid();
        TestWebAppFactory.OrderServiceMock.GetOrderStatus(id)
            .Returns(async _ =>
            {
                await using var connection = new NpgsqlConnection(database.GetConnectionString());
                await connection.ExecuteAsync("DO $$ BEGIN RAISE EXCEPTION 'private-database-message'; END $$;");
                return WorkOrderStatus.Received;
            });
        using var negative = await TestClient.GetAsync($"/Orders/{id}/status?password=private-password");
        Telemetry.Flush();
        var received = false;
        string output = "";
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var logs = await collector.GetLogsAsync();
            output = logs.Stdout + logs.Stderr;
            received = output.Contains("ResourceSpans", StringComparison.Ordinal)
                && output.Contains("ResourceMetrics", StringComparison.Ordinal)
                && output.Contains("ResourceLog", StringComparison.Ordinal)
                && output.Contains(traceId, StringComparison.Ordinal)
                && output.Contains("Falha na operação PostgreSQL.", StringComparison.Ordinal)
                && output.Contains("Validação local da exportação.", StringComparison.Ordinal);
            received = received && output.Contains("mecanica.orders.created", StringComparison.Ordinal)
                && output.Contains("mecanica.orders.notifications", StringComparison.Ordinal)
                && output.Contains("mecanica.orders.stage.duration", StringComparison.Ordinal)
                && output.Contains("mecanica.orders.service.duration", StringComparison.Ordinal)
                && output.Contains("mecanica.orders.processing.failures", StringComparison.Ordinal);
            if (received) break;
            await Task.Delay(200);
        }
        Assert.That(received, Is.True,
            $"Collector signals: traces={output.Contains("ResourceSpans", StringComparison.Ordinal)}, "
            + $"metrics={output.Contains("ResourceMetrics", StringComparison.Ordinal)}, logs={output.Contains("ResourceLog", StringComparison.Ordinal)}, "
            + $"context={output.Contains(traceId, StringComparison.Ordinal)}, message={output.Contains("Validação local da exportação.", StringComparison.Ordinal)}.");
        Assert.That(output.Contains("mecanica-api", StringComparison.Ordinal), Is.True);
        Assert.That(output.Contains("service.version", StringComparison.Ordinal), Is.True);
        Assert.That(output.Contains("deployment.environment.name", StringComparison.Ordinal), Is.True);
        Assert.That(output.Contains("http.server.request.duration", StringComparison.Ordinal), Is.True);
        Assert.That(output.Contains("exception.stacktrace", StringComparison.Ordinal), Is.False);
        Assert.That(output.Contains("RAISE EXCEPTION", StringComparison.Ordinal), Is.False);
        Assert.That(output.Contains("private-", StringComparison.Ordinal), Is.False, "Sensitive inputs must not reach the Collector.");
        Assert.That(output.Contains(id.ToString(), StringComparison.Ordinal), Is.False, "Raw route IDs must not reach the Collector.");
        Assert.That(output.Contains(completedOrderId.ToString(), StringComparison.Ordinal), Is.False);
        Assert.That(Regex.Count(output, @"Trace ID\s*:\s*" + traceId), Is.EqualTo(1), "The successful request span must be exported only once.");
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(creation.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(negative.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        await collector.StopAsync();
        logger.LogInformation("Exportador local indisponível.");
        Telemetry.Flush();
        using var second = await TestClient.GetAsync("/Customers");
        using var creationWithoutCollector = await TestClient.PostAsJsonAsync("/Orders", new
        { customerId = Guid.NewGuid(), vehicleId = Guid.NewGuid(), services = Array.Empty<object>(), materials = Array.Empty<object>() });
        using var ready = await TestClient.GetAsync("/health/ready");
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(creationWithoutCollector.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(ready.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
