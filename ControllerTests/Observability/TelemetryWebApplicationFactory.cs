using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Service.Interface;
using System.Diagnostics;

namespace ControllerTests.Observability;

public sealed class TelemetryWebApplicationFactory(string? connectionString = null, bool realCustomers = false)
    : TestWebApplicationFactory(connectionString)
{
    public TelemetryBuffer<Activity> Spans { get; } = new();
    public TelemetryBuffer<LogRecord> Logs { get; } = new();
    public TelemetryBuffer<Metric> Metrics { get; } = new();
    public Action<IServiceCollection>? ConfigureTestServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(Spans));
            services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddInMemoryExporter(Metrics));
            services.Configure<OpenTelemetryLoggerOptions>(options => options.AddInMemoryExporter(Logs));
            if (realCustomers)
            {
                services.RemoveAll<ICustomerService>();
                services.AddScoped<ICustomerService, Service.CustomerService>();
            }
            ConfigureTestServices?.Invoke(services);
        });
    }

    public void Flush()
    {
        Services.GetRequiredService<TracerProvider>().ForceFlush(5_000);
        Services.GetRequiredService<MeterProvider>().ForceFlush(5_000);
        Services.GetRequiredService<LoggerProvider>().ForceFlush(5_000);
    }
}
