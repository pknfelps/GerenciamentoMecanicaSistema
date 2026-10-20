using Infrastructure.Observability;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace GerenciamentoMecanicaSistema.Observability;

public sealed class ApiMetricProvider : IDisposable
{
    public MeterProvider Provider { get; }
    public IntegrationMetrics? Integrations { get; }
    private readonly int shutdownTimeoutMilliseconds;
    private int disposed;

    private ApiMetricProvider(MeterProvider provider, IntegrationMetrics? integrations, int shutdownTimeoutMilliseconds)
    {
        Provider = provider;
        Integrations = integrations;
        this.shutdownTimeoutMilliseconds = shutdownTimeoutMilliseconds;
    }

    public static ApiMetricProvider? Create(WebApplicationBuilder builder)
    {
        var settings = TelemetrySettings.Read(builder.Configuration);
        if (!settings.Enabled)
            return null;

        var configuration = builder.Configuration;
        // Let the SDK append each signal path, including when the legacy setting supplied the base URL.
        configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = settings.Endpoint!.AbsoluteUri;
        var metrics = Sdk.CreateMeterProviderBuilder()
            .ConfigureServices(services => services.AddSingleton<IConfiguration>(configuration))
            .SetResourceBuilder(ResourceBuilder.CreateEmpty()
                .AddService(configuration["OTEL_SERVICE_NAME"] ?? builder.Environment.ApplicationName,
                    serviceVersion: settings.Version, autoGenerateServiceInstanceId: false)
                .AddTelemetrySdk()
                .AddAttributes([new("deployment.environment.name", settings.Environment)])
                .AddEnvironmentVariableDetector());

        foreach (var meter in configuration.GetSection("Metrics:EnabledMetrics").GetChildren().Where(meter => meter.Get<bool>()))
            metrics.AddMeter(meter.Key);

        metrics.AddView("http.server.*", new MetricStreamConfiguration { TagKeys = settings.Collection.HttpMetricAttributes });
        if (configuration.GetValue<bool>($"Metrics:EnabledMetrics:{OrderBusinessMetrics.MeterName}"))
        {
            var business = configuration.GetSection("Telemetry:BusinessMetrics").Get<BusinessMetricOptions>()
                ?? throw new InvalidOperationException("Configuração das métricas de negócio ausente.");
            business.Validate();
            metrics.AddView(business.CreateView);
        }

        metrics.AddView(IntegrationMetrics.InstrumentName, new MetricStreamConfiguration { TagKeys = settings.IntegrationMetricAttributes });
        metrics.AddOtlpExporter((exporter, reader) =>
        {
            exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
            exporter.TimeoutMilliseconds = configuration.GetValue<int>("OTEL_EXPORTER_OTLP_TIMEOUT");
            reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = configuration.GetValue<int>("OTEL_METRIC_EXPORT_INTERVAL");
            reader.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds = configuration.GetValue<int>("OTEL_METRIC_EXPORT_TIMEOUT");
            reader.TemporalityPreference = configuration["OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE"] == "delta"
                ? MetricReaderTemporalityPreference.Delta : MetricReaderTemporalityPreference.Cumulative;
        });

        var provider = metrics.Build();
        try
        {
            var integrations = configuration.GetValue<bool>($"Metrics:EnabledMetrics:{IntegrationMetrics.MeterName}")
                ? new IntegrationMetrics() : null;
            return new(provider, integrations, settings.StartupShutdownTimeoutMilliseconds);
        }
        catch
        {
            provider.Shutdown(settings.StartupShutdownTimeoutMilliseconds);
            provider.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        try
        {
            // Shutdown collects the last measurements and uses the native timeout once, including startup failures.
            if (!Provider.Shutdown(shutdownTimeoutMilliseconds))
                Console.Error.WriteLine("Não foi possível concluir a exportação das métricas no prazo.");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("Falha ao encerrar métricas. ExceptionType: {0}", error.GetType().FullName);
        }
        finally
        {
            try { Provider.Dispose(); }
            catch (Exception error)
            {
                Console.Error.WriteLine("Falha ao descartar provider de métricas. ExceptionType: {0}", error.GetType().FullName);
            }
            try { Integrations?.Dispose(); }
            catch (Exception error)
            {
                Console.Error.WriteLine("Falha ao descartar instrumentos de integração. ExceptionType: {0}", error.GetType().FullName);
            }
        }
    }
}
