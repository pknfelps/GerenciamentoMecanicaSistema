using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Infrastructure.Persistence.PostgreSql.Observability;
using Service.Interface.Observability;
using Microsoft.AspNetCore.Mvc;

namespace GerenciamentoMecanicaSistema.Observability;

public static class ObservabilityDependencyInjection
{
    public static WebApplicationBuilder AddApiObservability(this WebApplicationBuilder builder)
    {
        var settings = TelemetrySettings.Read(builder.Configuration);
        if (!settings.Enabled)
            return builder;

        Sdk.SetDefaultTextMapPropagator(new GatewayTracePropagator(settings.InboundFormats));

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(settings.Collection);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHostedService<TelemetryExceptionRecorder>();
        var businessEnabled = builder.Configuration.GetValue<bool>($"Metrics:EnabledMetrics:{OrderBusinessMetrics.MeterName}");
        if (businessEnabled)
        {
            var business = builder.Configuration.GetSection("Telemetry:BusinessMetrics").Get<BusinessMetricOptions>()
                ?? throw new InvalidOperationException("Configuração das métricas de negócio ausente.");
            business.Validate();
            builder.Services.AddSingleton(business);
            builder.Services.AddSingleton<OrderBusinessMetrics>();
            builder.Services.AddSingleton<IOrderNotificationMetrics>(services => services.GetRequiredService<OrderBusinessMetrics>());
            builder.Services.AddSingleton<IOrderMetricsHistoryReader>(_ => new OrderMetricsHistoryReader(
                builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Conexão de leitura das métricas não configurada.")));
            builder.Services.AddScoped<OrderBusinessMetricsFilter>();
            builder.Services.Configure<MvcOptions>(options => options.Filters.AddService<OrderBusinessMetricsFilter>());
        }
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.Clear()
                .AddService(builder.Environment.ApplicationName, serviceVersion: settings.Version, autoGenerateServiceInstanceId: false)
                .AddTelemetrySdk()
                .AddAttributes([new("deployment.environment.name", settings.Environment)])
                .AddEnvironmentVariableDetector())
            .WithTracing(tracing => tracing
                .SetSampler(new ApiTelemetrySampler(new HttpContextAccessor(), settings.Collection))
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context => !TelemetryRequest.IsProbe(context, settings.Collection);
                    options.RecordException = false;
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.SetTag("http.request.method", TelemetryRequest.Method(response.HttpContext.Request.Method, settings.Collection));
                        activity.SetTag("http.route", TelemetryRequest.Route(response.HttpContext));
                    };
                })
                .AddNpgsql()
                .AddProcessor(new SafeTraceProcessor(settings.Collection)))
            .WithMetrics(metrics =>
            {
                metrics.AddView("http.server.*", new MetricStreamConfiguration { TagKeys = settings.Collection.HttpMetricAttributes });
                if (businessEnabled)
                {
                    var business = builder.Configuration.GetSection("Telemetry:BusinessMetrics").Get<BusinessMetricOptions>()!;
                    metrics.AddView(business.CreateView);
                }
            })
            .UseOtlpExporter(OtlpExportProtocol.HttpProtobuf, settings.Endpoint!);

        builder.Logging.AddOpenTelemetry(options =>
        {
            builder.Configuration.GetSection("Logging:OpenTelemetry").Bind(options);
            options.IncludeScopes = false;
        });
        return builder;
    }
}
