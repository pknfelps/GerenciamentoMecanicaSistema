using GerenciamentoMecanicaSistema.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.Metrics;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class OrderNotificationMetricsTests
{
    [TestCase("local", "smtp", true)]
    [TestCase("hom", "simulated", true)]
    [TestCase("prd", "simulated", false)]
    public void NotificationModesDistinguishSimulationFromSmtp(string environment, string mode, bool success)
    {
        using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var listener = new MeterListener();
        var measurements = new List<Dictionary<string, object?>>();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Name == "mecanica.orders.notifications") current.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            Assert.That(value, Is.EqualTo(1));
            measurements.Add(tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value));
        });
        listener.Start();
        var metrics = new OrderBusinessMetrics(services.GetRequiredService<IMeterFactory>(), Settings(environment), NullLogger<OrderBusinessMetrics>.Instance);
        metrics.RecordNotification(true, success);
        Assert.That(measurements, Has.Count.EqualTo(1));
        Assert.That(measurements[0], Is.EquivalentTo(new Dictionary<string, object?>
        {
            ["notification.type"] = "budget_available", ["outcome"] = success ? "success" : "failure", ["mode"] = mode
        }));
    }

    [Test]
    public void MeasurementFailureDoesNotPropagateAndFactoryDisposesInstruments()
    {
        var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var listener = new MeterListener();
        var disposed = 0;
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Meter.Name == OrderBusinessMetrics.MeterName) current.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, _, _) => throw new InvalidOperationException("private-measurement-failure"));
        listener.MeasurementsCompleted = (_, _) => disposed++;
        listener.Start();
        try
        {
            var metrics = new OrderBusinessMetrics(services.GetRequiredService<IMeterFactory>(), Settings("local"), NullLogger<OrderBusinessMetrics>.Instance);
            Assert.DoesNotThrow(metrics.RecordCreated);
            Assert.DoesNotThrow(() => metrics.RecordNotification(true, true));
            Assert.DoesNotThrow(() => metrics.RecordProcessingFailure("CompleteExecution"));
        }
        finally { services.Dispose(); }
        Assert.That(disposed, Is.EqualTo(6));
    }

    [TestCase(0)]
    [TestCase(6)]
    public void InvalidHistoryTimeoutHasFixedValidationMessage(int seconds)
    {
        var options = new BusinessMetricOptions
        { HistoryQueryTimeoutSeconds = seconds, DurationBoundariesSeconds = [1, 5], Attributes = ["stage"] };
        Assert.That(() => options.Validate(), Throws.InvalidOperationException.With.Message.EqualTo("Configuração das métricas de negócio inválida."));
    }

    [TestCase("duplicate")]
    [TestCase("unordered")]
    [TestCase("negative")]
    [TestCase("infinite")]
    public void InvalidHistogramBoundariesFailValidation(string scenario)
    {
        var boundaries = scenario switch
        { "duplicate" => new[] { 1d, 1d }, "unordered" => [5d, 1d], "negative" => [-1d, 1d], _ => [1d, double.PositiveInfinity] };
        var options = new BusinessMetricOptions
        { HistoryQueryTimeoutSeconds = 1, DurationBoundariesSeconds = boundaries, Attributes = ["stage"] };
        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    private static TelemetrySettings Settings(string environment) => TelemetrySettings.Read(new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(TestContext.CurrentContext.TestDirectory, "Observability", "observability.json"))
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true", ["Runtime:Environment"] = environment,
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:1"
        }).Build());
}
