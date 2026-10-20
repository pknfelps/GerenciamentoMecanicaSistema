using GerenciamentoMecanicaSistema.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class ObservabilityRegistrationTests
{
    private static readonly string[] ExpectedInboundFormats = ["xray", "tracecontext"];
    private TextMapPropagator originalPropagator = null!;

    [SetUp]
    public void SaveGlobalPropagator() => originalPropagator = Propagators.DefaultTextMapPropagator;

    [TearDown]
    public void RestoreGlobalPropagator() => Sdk.SetDefaultTextMapPropagator(originalPropagator);

    [Test]
    public void DisabledTelemetryLeavesGlobalPropagatorUnchanged()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());

        builder.AddApiObservability();

        Assert.Multiple(() =>
        {
            Assert.That(Propagators.DefaultTextMapPropagator, Is.SameAs(originalPropagator));
            Assert.That(builder.Services.Any(service => service.ServiceType == typeof(TelemetrySettings)), Is.False);
        });
    }

    [Test]
    public void EnabledTelemetryRegistersGatewayPropagatorAndValidatedSettings()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.Configuration.AddJsonFile(Path.Combine(TestContext.CurrentContext.TestDirectory, "Observability", "observability.json"));
        builder.Configuration["Telemetry:Enabled"] = "true";
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318";

        builder.AddApiObservability();

        // Resolve only configuration. No host or telemetry provider is started.
        using var services = builder.Services.BuildServiceProvider();
        var settings = services.GetRequiredService<TelemetrySettings>();
        Assert.Multiple(() =>
        {
            Assert.That(Propagators.DefaultTextMapPropagator, Is.TypeOf<GatewayTracePropagator>());
            Assert.That(settings.InboundFormats, Is.EqualTo(ExpectedInboundFormats));
        });
    }

    [TestCase("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318")]
    [TestCase("OTEL_EXPORTER_OTLP_ENDPOINT", "https://collector.example.invalid/otlp/")]
    [TestCase("Telemetry:OtlpEndpoint", "http://localhost:4318")]
    [TestCase("Telemetry:OtlpEndpoint", "https://collector.example.invalid/otlp/")]
    public void StandardAndLegacyBaseEndpointsAreAvailableToTheNativeExporterConfiguration(string key, string endpoint)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.Configuration.AddJsonFile(Path.Combine(TestContext.CurrentContext.TestDirectory, "Observability", "observability.json"));
        builder.Configuration["Telemetry:Enabled"] = "true";
        builder.Configuration[key] = endpoint;

        builder.AddApiObservability();

        // Read SDK options without starting a host, providers or network requests.
        using var services = builder.Services.BuildServiceProvider();
        var exporter = services.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>().CurrentValue;

        Assert.That(exporter.Endpoint, Is.EqualTo(new Uri(endpoint)));
    }
}
