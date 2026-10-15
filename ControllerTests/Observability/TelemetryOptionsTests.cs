using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class TelemetryOptionsTests : BaseControllerTests
{
    protected override TestWebApplicationFactory CreateFactory() => new TelemetryWebApplicationFactory
    {
        TelemetryEnabled = "true",
        TelemetryEndpoint = "private-invalid-legacy-endpoint",
        TelemetryConfiguration =
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:1",
            ["OTEL_SERVICE_NAME"] = "mecanica-api-configured",
            ["OTEL_RESOURCE_ATTRIBUTES"] = "service.version=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa,deployment.environment.name=hom,service.namespace=oficina",
            ["OTEL_EXPORTER_OTLP_TIMEOUT"] = "1700",
            ["OTEL_BSP_MAX_QUEUE_SIZE"] = "1024",
            ["OTEL_BSP_MAX_EXPORT_BATCH_SIZE"] = "256",
            ["OTEL_BSP_SCHEDULE_DELAY"] = "1500",
            ["OTEL_BSP_EXPORT_TIMEOUT"] = "1700",
            ["OTEL_BLRP_MAX_QUEUE_SIZE"] = "512",
            ["OTEL_BLRP_MAX_EXPORT_BATCH_SIZE"] = "128",
            ["OTEL_BLRP_SCHEDULE_DELAY"] = "1600",
            ["OTEL_BLRP_EXPORT_TIMEOUT"] = "1800",
            ["OTEL_METRIC_EXPORT_INTERVAL"] = "45000",
            ["OTEL_METRIC_EXPORT_TIMEOUT"] = "1900",
            ["OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE"] = "cumulative"
        }
    };

    protected override void MockService() { }

    [Test]
    public void StandardResourceConfigurationAppliesToEverySignal()
    {
        var services = TestWebAppFactory.Services;
        BaseProvider[] providers =
        [services.GetRequiredService<TracerProvider>(), services.GetRequiredService<MeterProvider>(), services.GetRequiredService<LoggerProvider>()];
        foreach (var provider in providers)
        {
            var attributes = provider.GetResource().Attributes.ToDictionary(x => x.Key, x => x.Value);
            Assert.Multiple(() =>
            {
                Assert.That(attributes["service.name"], Is.EqualTo("mecanica-api-configured"));
                Assert.That(attributes["service.version"], Is.EqualTo(new string('a', 40)));
                Assert.That(attributes["deployment.environment.name"], Is.EqualTo("hom"));
                Assert.That(attributes["service.namespace"], Is.EqualTo("oficina"));
                Assert.That(attributes.ContainsKey("service.instance.id"), Is.False);
            });
        }
    }

    [Test]
    public void StandardExportConfigurationControlsBatchesTimeoutsAndMetrics()
    {
        var services = TestWebAppFactory.Services;
        var traces = services.GetRequiredService<IOptionsMonitor<ActivityExportProcessorOptions>>().CurrentValue.BatchExportProcessorOptions;
        var logs = services.GetRequiredService<IOptionsMonitor<LogRecordExportProcessorOptions>>().CurrentValue.BatchExportProcessorOptions;
        var metrics = services.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().CurrentValue;
        var exporter = services.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>().CurrentValue;
        Assert.Multiple(() =>
        {
            Assert.That(exporter.TimeoutMilliseconds, Is.EqualTo(1700));
            Assert.That(traces.MaxQueueSize, Is.EqualTo(1024));
            Assert.That(traces.MaxExportBatchSize, Is.EqualTo(256));
            Assert.That(traces.ScheduledDelayMilliseconds, Is.EqualTo(1500));
            Assert.That(traces.ExporterTimeoutMilliseconds, Is.EqualTo(1700));
            Assert.That(logs.MaxQueueSize, Is.EqualTo(512));
            Assert.That(logs.MaxExportBatchSize, Is.EqualTo(128));
            Assert.That(logs.ScheduledDelayMilliseconds, Is.EqualTo(1600));
            Assert.That(logs.ExporterTimeoutMilliseconds, Is.EqualTo(1800));
            Assert.That(metrics.TemporalityPreference, Is.EqualTo(MetricReaderTemporalityPreference.Cumulative));
            Assert.That(metrics.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds, Is.EqualTo(45000));
            Assert.That(metrics.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds, Is.EqualTo(1900));
        });
    }
}
