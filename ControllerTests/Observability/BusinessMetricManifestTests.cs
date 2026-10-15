using GerenciamentoMecanicaSistema.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Service.Interface.Observability;
using System.Net;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class BusinessMetricManifestTests : BaseControllerTests
{
    private readonly IOrderMetricsHistoryReader reader = Substitute.For<IOrderMetricsHistoryReader>();
    private TelemetryWebApplicationFactory Telemetry => (TelemetryWebApplicationFactory)TestWebAppFactory;
    protected override TestWebApplicationFactory CreateFactory()
    {
        var factory = new TelemetryWebApplicationFactory
        {
            TelemetryEnabled = "true", TelemetryEndpoint = "http://127.0.0.1:1",
            ConfigureTestServices = services => { services.RemoveAll<IOrderMetricsHistoryReader>(); services.AddSingleton(reader); }
        };
        factory.TelemetryConfiguration["Telemetry:BusinessMetrics:HistoryQueryTimeoutSeconds"] = "2";
        for (var index = 0; index < 6; index++) factory.TelemetryConfiguration[$"Telemetry:BusinessMetrics:Attributes:{index}"] = "stage";
        for (var index = 0; index < 13; index++) factory.TelemetryConfiguration[$"Telemetry:BusinessMetrics:DurationBoundariesSeconds:{index}"] = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        reader.ReadAsync(Arg.Any<Guid>(), 2, Arg.Any<CancellationToken>()).Returns(OrderDurationMeasurementsTests.History());
        return factory;
    }
    protected override void MockService() { }

    [Test]
    public async Task ManifestControlsReadTimeoutDimensionsAndHistogramBoundaries()
    {
        using var response = await TestClient.PatchAsync($"/Orders/{Guid.NewGuid()}/delivery", null);
        Telemetry.Flush();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await reader.Received(1).ReadAsync(Arg.Any<Guid>(), 2, Arg.Is<CancellationToken>(token => token.CanBeCanceled));
        var metric = Telemetry.Metrics.Single(metric => metric.Name == "mecanica.orders.stage.duration");
        foreach (ref readonly var point in metric.GetMetricPoints())
        {
            var keys = new List<string>();
            foreach (var tag in point.Tags) keys.Add(tag.Key);
            Assert.That(keys, Is.EqualTo(new[] { "stage" }));
            var bounds = new List<double>();
            foreach (var bucket in point.GetHistogramBuckets()) bounds.Add(bucket.ExplicitBound);
            Assert.That(bounds, Is.EqualTo(Enumerable.Range(1, 13).Select(value => (double)value).Append(double.PositiveInfinity)));
        }
    }
}
