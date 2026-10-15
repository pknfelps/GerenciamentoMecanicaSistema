using Domain.Interface.Order;
using GerenciamentoMecanicaSistema.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System.Diagnostics;
using System.Net;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class TelemetryControllerTests : BaseControllerTests
{
    private TelemetryWebApplicationFactory Telemetry => (TelemetryWebApplicationFactory)TestWebAppFactory;
    protected override TestWebApplicationFactory CreateFactory() => new TelemetryWebApplicationFactory
    { TelemetryEnabled = "true", TelemetryEndpoint = "http://127.0.0.1:1" };
    protected override void MockService() { }

    [TestCase("01")]
    [TestCase("00")]
    public async Task HttpTracePreservesContextEvenForUnsampledParent(string flags)
    {
        var id = Guid.NewGuid();
        TestWebAppFactory.OrderServiceMock.GetOrderStatus(id).Returns(WorkOrderStatus.Received);
        var traceId = ActivityTraceId.CreateRandom().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/Orders/{id}/status?document=private-document");
        request.Headers.Add("traceparent", $"00-{traceId}-0123456789abcdef-{flags}");
        request.Headers.Add("baggage", "document=private-document");
        using var response = await TestClient.SendAsync(request);
        Telemetry.Flush();
        var span = Telemetry.Spans.Single(x => x.Kind == ActivityKind.Server);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(span.TraceId.ToString(), Is.EqualTo(traceId));
            Assert.That(span.Recorded, Is.True);
            Assert.That(span.DisplayName, Is.EqualTo("GET Orders/{id}/status"));
            Assert.That(span.ParentSpanId.ToString(), Is.EqualTo("0123456789abcdef"));
            Assert.That(span.Baggage, Is.Empty);
            Assert.That(span.TagObjects.Select(x => x.Key), Is.SubsetOf(new[]
            { "http.request.method", "http.route", "http.response.status_code", "network.protocol.version", "url.scheme", "error.type" }));
        });
    }

    [Test]
    public async Task ErrorLogAndTraceHaveSameContextWithoutExceptionContents()
    {
        var id = Guid.NewGuid();
        TestWebAppFactory.OrderServiceMock.GetOrderStatus(id).Returns<WorkOrderStatus>(_ =>
        {
            var error = new InvalidOperationException("private-message", new Exception("private-inner"));
            error.Data["password"] = "private-data";
            throw error;
        });
        using var response = await TestClient.GetAsync($"/Orders/{id}/status?password=private-query");
        Telemetry.Flush();
        var span = Telemetry.Spans.Single(x => x.Kind == ActivityKind.Server);
        var log = Telemetry.Logs.Single(x => x.CategoryName!.Contains("ExceptionHandlingMiddleware"));
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(log.TraceId, Is.EqualTo(span.TraceId));
            Assert.That(log.SpanId, Is.EqualTo(span.SpanId));
            Assert.That(log.Exception, Is.Null);
            Assert.That(log.FormattedMessage, Does.Contain("Erro inesperado durante a requisição.").And.Not.Contain("private-"));
            Assert.That(span.StatusDescription, Is.EqualTo("Falha durante a requisição HTTP."));
            Assert.That(string.Join(' ', span.TagObjects), Does.Not.Contain("private-"));
            Assert.That(span.Events, Is.Empty);
            Assert.That(Telemetry.Logs.All(x => x.CategoryName == typeof(GerenciamentoMecanicaSistema.Middleware.ExceptionHandlingMiddleware).FullName), Is.True);
        });
    }

    [Test]
    public async Task UnknownRouteUsesFixedNameAndMetricDimensions()
    {
        using var response = await TestClient.GetAsync("/private-person/417.384.220-11?password=private-password");
        Telemetry.Flush();
        var span = Telemetry.Spans.Single(x => x.Kind == ActivityKind.Server);
        var metric = Telemetry.Metrics.Single(x => x.Name == "http.server.request.duration");
        var tags = new List<string>();
        foreach (ref readonly var point in metric.GetMetricPoints())
            foreach (var tag in point.Tags) tags.Add(tag.Key);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(span.DisplayName, Is.EqualTo("GET unmatched"));
            Assert.That(string.Join(' ', span.TagObjects), Does.Not.Contain("private-").And.Not.Contain("417.384"));
            Assert.That(tags, Is.SubsetOf(new[] { "http.request.method", "http.route", "http.response.status_code", "network.protocol.version", "url.scheme" }));
        });
    }

    [Test]
    public async Task ConcurrentRequestsKeepTheirOwnCorrelationAndDoNotDuplicateSpans()
    {
        TestWebAppFactory.OrderServiceMock.GetOrderStatus(Arg.Any<Guid>()).Returns<WorkOrderStatus>(_ => throw new InvalidOperationException("private-message"));
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => TestClient.GetAsync($"/Orders/{Guid.NewGuid()}/status")));
        foreach (var response in responses) response.Dispose();
        Telemetry.Flush();
        var spans = Telemetry.Spans.Where(x => x.Kind == ActivityKind.Server).ToArray();
        var logs = Telemetry.Logs.Where(x => x.CategoryName!.Contains("ExceptionHandlingMiddleware")).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(spans, Has.Length.EqualTo(6));
            Assert.That(logs, Has.Length.EqualTo(6));
            Assert.That(spans.Select(x => x.TraceId).Distinct().Count(), Is.EqualTo(6));
            Assert.That(logs.All(log => spans.Count(span => span.TraceId == log.TraceId && span.SpanId == log.SpanId) == 1), Is.True);
        });
    }

    [Test]
    public async Task ResourceRuntimeMetricsAndProviderFilterAreConsistent()
    {
        var loggerFactory = TestWebAppFactory.Services.GetRequiredService<ILoggerFactory>();
        loggerFactory.CreateLogger("Infrastructure.Email.LoggingEmailSender").LogInformation("Envio de e-mail simulado.");
        loggerFactory.CreateLogger("Microsoft.AspNetCore.Private").LogWarning("framework-private");
        loggerFactory.CreateLogger("Npgsql.Private").LogWarning("driver-private");
        using var response = await TestClient.GetAsync("/health/live");
        Telemetry.Flush();
        var resource = Telemetry.Spans.Count == 0
            ? TestWebAppFactory.Services.GetRequiredService<OpenTelemetry.Trace.TracerProvider>().GetResource()
            : throw new InvalidOperationException("Probe trace must be absent");
        var attrs = resource.Attributes.ToDictionary(x => x.Key, x => x.Value);
        Assert.Multiple(() =>
        {
            Assert.That(attrs["service.name"], Is.EqualTo("mecanica-api"));
            Assert.That(attrs["deployment.environment.name"], Is.EqualTo("local"));
            Assert.That(attrs["service.version"].ToString(), Does.Match("^([a-f0-9]{40}|local)$"));
            Assert.That(Telemetry.Logs, Has.Count.EqualTo(1));
            Assert.That(Telemetry.Logs.Single().FormattedMessage, Is.EqualTo("Envio de e-mail simulado."));
            Assert.That(Telemetry.Metrics.Any(x => x.MeterName == "System.Runtime"), Is.True);
            Assert.That(Telemetry.Metrics.Any(x => x.Name == "http.server.request.duration"), Is.True);
        });
    }

    [Test]
    public async Task LogScopesAreNotExportedAndHttpMetricsCountRequestsOnce()
    {
        var logger = TestWebAppFactory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Service.ScopeValidation");
        using (logger.BeginScope(new Dictionary<string, object?> { ["document"] = "private-document", ["password"] = "private-password" }))
            logger.LogInformation("Operação local concluída.");
        using var first = await TestClient.GetAsync("/Customers");
        using var second = await TestClient.GetAsync("/Customers");
        Telemetry.Flush();
        var metric = Telemetry.Metrics.Single(x => x.Name == "http.server.request.duration");
        long count = 0;
        foreach (ref readonly var point in metric.GetMetricPoints()) count += point.GetHistogramCount();
        var log = Telemetry.Logs.Single(x => x.CategoryName == "Service.ScopeValidation");
        var scopes = new List<object?>();
        log.ForEachScope((scope, state) => state.Add(scope.Scope), scopes);
        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(Telemetry.Spans.Count(x => x.Kind == ActivityKind.Server), Is.EqualTo(2));
            Assert.That(scopes, Is.Empty);
            Assert.That(log.FormattedMessage, Is.EqualTo("Operação local concluída."));
            Assert.That(string.Join(' ', log.Attributes ?? []), Does.Not.Contain("private-"));
            Assert.That(Telemetry.Metrics.Any(x => x.Name == "http.server.active_requests"), Is.True);
        });
    }

    [Test]
    public async Task HostDisposalRemovesActivityListeners()
    {
        var services = TestWebAppFactory.Services;
        TestClient.Dispose();
        await TestWebAppFactory.DisposeAsync();
        var spanCount = Telemetry.Spans.Count;
        using var observer = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Npgsql",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(observer);
        using var source = new ActivitySource("Npgsql");
        using (var span = source.StartActivity("after-disposal")!)
        {
            span.AddException(new InvalidOperationException("local-disposal-check"));
            Assert.That(span.Events.Single().Tags.Single(x => x.Key == "exception.message").Value,
                Is.EqualTo("local-disposal-check"));
        }
        Assert.That(Telemetry.Spans.Count, Is.EqualTo(spanCount));
        Assert.Throws<ObjectDisposedException>(() => services.GetRequiredService<OpenTelemetry.Trace.TracerProvider>());
    }
}
