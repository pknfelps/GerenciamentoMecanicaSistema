using Domain.Interface.Order;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Diagnostics;
using System.Net;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class TelemetryCollectionTests : BaseControllerTests
{
    private TelemetryWebApplicationFactory Telemetry => (TelemetryWebApplicationFactory)TestWebAppFactory;

    protected override TestWebApplicationFactory CreateFactory() => new TelemetryWebApplicationFactory
    {
        TelemetryEnabled = "true",
        TelemetryEndpoint = "http://127.0.0.1:1",
        TelemetryConfiguration =
        {
            ["Telemetry:Collection:ExcludedPaths:3"] = "/Customers",
            ["Telemetry:Collection:HttpTraceAttributes:0"] = "http.request.method",
            ["Telemetry:Collection:HttpTraceAttributes:1"] = "http.route",
            ["Telemetry:Collection:HttpTraceAttributes:2"] = "http.request.method",
            ["Telemetry:Collection:HttpTraceAttributes:3"] = "http.route",
            ["Telemetry:Collection:HttpTraceAttributes:4"] = "http.request.method",
            ["Telemetry:Collection:HttpTraceAttributes:5"] = "http.route",
            ["Telemetry:Collection:HttpMetricAttributes:0"] = "http.route",
            ["Telemetry:Collection:HttpMetricAttributes:1"] = "http.route",
            ["Telemetry:Collection:HttpMetricAttributes:2"] = "http.route",
            ["Telemetry:Collection:HttpMetricAttributes:3"] = "http.route",
            ["Telemetry:Collection:HttpMetricAttributes:4"] = "http.route",
            ["Logging:OpenTelemetry:LogLevel:Service.CollectionValidation"] = "Warning",
            ["Logging:OpenTelemetry:IncludeScopes"] = "true",
            ["Metrics:EnabledMetrics:System.Runtime"] = "false"
        }
    };

    protected override void MockService() { }

    [Test]
    public async Task DeclarativeExclusionDropsRequestTraceAndPreservesMetrics()
    {
        using var response = await TestClient.GetAsync("/Customers");
        Telemetry.Flush();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(Telemetry.Spans, Is.Empty);
            Assert.That(Telemetry.Metrics.Any(x => x.Name == "http.server.request.duration"), Is.True);
        });
    }

    [Test]
    public async Task DeclarativeAttributesLimitTracesBeforeExport()
    {
        var id = Guid.NewGuid();
        TestWebAppFactory.OrderServiceMock.GetOrderStatus(id).Returns(WorkOrderStatus.Received);
        using var response = await TestClient.GetAsync($"/Orders/{id}/status?document=private-document");
        Telemetry.Flush();
        var span = Telemetry.Spans.Single(x => x.Kind == ActivityKind.Server);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(span.DisplayName, Is.EqualTo("GET Orders/{id}/status"));
            Assert.That(span.TagObjects.Select(x => x.Key), Is.EquivalentTo(new[] { "http.request.method", "http.route" }));
            Assert.That(string.Join(' ', span.TagObjects), Does.Not.Contain("private-document").And.Not.Contain(id.ToString()));
        });
    }

    [Test]
    public void DeclarativeLogLevelsApplyWithoutAllowingUnsafeScopes()
    {
        var logger = TestWebAppFactory.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Service.CollectionValidation");
        using (logger.BeginScope(new Dictionary<string, object?> { ["password"] = "private-scope" }))
        {
            logger.LogInformation("Mensagem informativa.");
            logger.LogWarning("Aviso técnico.");
        }
        Telemetry.Flush();
        var log = Telemetry.Logs.Single();
        var scopes = new List<object?>();
        log.ForEachScope((scope, values) => values.Add(scope.Scope), scopes);
        Assert.Multiple(() =>
        {
            Assert.That(log.FormattedMessage, Is.EqualTo("Aviso técnico."));
            Assert.That(scopes, Is.Empty);
            Assert.That(string.Join(' ', log.Attributes ?? []), Does.Not.Contain("private-scope"));
        });
    }

    [Test]
    public async Task DeclarativeMeterSelectionAndDimensionsApplyBeforeAggregation()
    {
        using var response = await TestClient.GetAsync("/Customers");
        Telemetry.Flush();
        var metric = Telemetry.Metrics.Single(x => x.Name == "http.server.request.duration");
        var keys = new List<string>();
        foreach (ref readonly var point in metric.GetMetricPoints())
            foreach (var tag in point.Tags) keys.Add(tag.Key);
        Assert.Multiple(() =>
        {
            Assert.That(keys.Distinct(), Is.EquivalentTo(new[] { "http.route" }));
            Assert.That(Telemetry.Metrics.Any(x => x.MeterName == "System.Runtime"), Is.False);
        });
    }
}
