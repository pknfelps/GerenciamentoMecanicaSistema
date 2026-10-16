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
    private OtlpTestEndpoint? destination;
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
        var newRelic = (await File.ReadAllTextAsync(configuration.FullName)).Contains("otlphttp/newrelic", StringComparison.Ordinal);
        if (newRelic) destination = await OtlpTestEndpoint.StartAsync();
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
                .WithCommand(newRelic
                    ? new[] { $"--config=/etc/otelcol-contrib/{configuration.Name}",
                       $"--config=yaml:exporters::otlphttp/newrelic::endpoint: {destination!.CollectorEndpoint}",
                       $"--config=yaml:exporters::otlphttp/newrelic::headers::api-key: {OtlpTestEndpoint.License}" }
                    : new[] { $"--config=/etc/otelcol-contrib/{configuration.Name}", "--config=yaml:exporters::debug::verbosity: detailed" })
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
        if (destination is not null) await destination.DisposeAsync();
    }

    protected override TestWebApplicationFactory CreateFactory() => new TelemetryWebApplicationFactory(database.GetConnectionString())
    {
        TelemetryEnabled = "true",
        TelemetryEndpoint = $"http://{collector.Hostname}:{collector.GetMappedPublicPort(4318)}"
    };
    protected override void MockService() { }

    private async Task VerifyDestinationFailuresAsync(ILogger logger)
    {
        foreach (var status in new[] { 401, 429, 503 })
        {
            destination!.StatusCode = status;
            using var response = await TestClient.GetAsync("/Customers");
            logger.LogInformation("Destino de telemetria com falha controlada.");
            Telemetry.Flush();
            for (var attempt = 0; attempt < 40 && !destination.Requests.Any(item => item.Status == status); attempt++)
                await Task.Delay(100);
            Assert.That(destination.Requests.Any(item => item.Status == status), Is.True);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            destination.StatusCode = 200;
            await Task.Delay(1200);
        }
        destination!.Delay = TimeSpan.FromSeconds(4);
        using var slow = await TestClient.GetAsync("/Customers");
        Telemetry.Flush();
        Assert.That(slow.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await Task.Delay(3200);
        destination.Delay = TimeSpan.Zero;
        await destination.StopAsync();
        using var offline = await TestClient.GetAsync("/Customers");
        using var ready = await TestClient.GetAsync("/health/ready");
        Telemetry.Flush();
        Assert.That(offline.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(ready.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var captured = await collector.GetLogsAsync();
        Assert.That(captured.Stdout + captured.Stderr, Does.Not.Contain(OtlpTestEndpoint.License));
    }

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
            if (destination is not null)
            {
                output = destination.PayloadText;
                received = destination.HasAllSignals && output.Contains("mecanica.orders.created", StringComparison.Ordinal)
                    && output.Contains("mecanica.orders.notifications", StringComparison.Ordinal)
                    && output.Contains("mecanica.orders.stage.duration", StringComparison.Ordinal)
                    && output.Contains("mecanica.orders.service.duration", StringComparison.Ordinal)
                    && output.Contains("mecanica.orders.processing.failures", StringComparison.Ordinal);
                if (received) break;
                await Task.Delay(200);
                continue;
            }
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
        var diagnostic = destination?.DescribeReceivedSignals()
            ?? $"traces={output.Contains("ResourceSpans", StringComparison.Ordinal)}, "
            + $"metrics={output.Contains("ResourceMetrics", StringComparison.Ordinal)}, "
            + $"logs={output.Contains("ResourceLog", StringComparison.Ordinal)}, "
            + $"context={output.Contains(traceId, StringComparison.Ordinal)}, "
            + $"message={output.Contains("Validação local da exportação.", StringComparison.Ordinal)}";
        Assert.That(received, Is.True, $"Collector={collector.State}; {diagnostic}.");
        Assert.That(output.Contains("mecanica-api", StringComparison.Ordinal), Is.True);
        Assert.That(output.Contains("service.version", StringComparison.Ordinal), Is.True);
        Assert.That(output.Contains("deployment.environment.name", StringComparison.Ordinal), Is.True);
        Assert.That(output.Contains("http.server.request.duration", StringComparison.Ordinal), Is.True);
        Assert.That(output.Contains("exception.stacktrace", StringComparison.Ordinal), Is.False);
        Assert.That(output.Contains("RAISE EXCEPTION", StringComparison.Ordinal), Is.False);
        Assert.That(output.Contains("private-", StringComparison.Ordinal), Is.False, "Sensitive inputs must not reach the Collector.");
        Assert.That(output.Contains(id.ToString(), StringComparison.Ordinal), Is.False, "Raw route IDs must not reach the Collector.");
        Assert.That(output.Contains(completedOrderId.ToString(), StringComparison.Ordinal), Is.False);
        if (destination is null)
            Assert.That(Regex.Count(output, @"Trace ID\s*:\s*" + traceId), Is.EqualTo(1), "The successful request span must be exported only once.");
        else
        {
            Assert.That(destination.Requests.All(item => item.ApiKey == OtlpTestEndpoint.License), Is.True);
            Assert.That(output, Does.Not.Contain(OtlpTestEndpoint.License));
            var traceBytes = Convert.FromHexString(traceId);
            Assert.That(destination.Requests.Any(item => item.Path == "/v1/traces" && item.Body.AsSpan().IndexOf(traceBytes) >= 0), Is.True);
            var errorTraceBytes = Convert.FromHexString(Telemetry.Spans.Single(span =>
                span.Kind == System.Diagnostics.ActivityKind.Server && span.Status == System.Diagnostics.ActivityStatusCode.Error).TraceId.ToString());
            Assert.That(destination.Requests.Any(item => item.Path == "/v1/logs" && item.Body.AsSpan().IndexOf(errorTraceBytes) >= 0), Is.True);
            Assert.That(destination.Requests.All(item => item.Body.Length < 1_000_000), Is.True);
            await VerifyDestinationFailuresAsync(logger);
        }
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
