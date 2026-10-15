using GerenciamentoMecanicaSistema.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ControllerTests.Observability;

public sealed class TelemetryConfigurationTests
{
    [TestCase(null)]
    [TestCase("false")]
    public void DisabledDoesNotRegisterProvidersOrReadEndpoint(string? enabled)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = enabled,
            ["Telemetry:OtlpEndpoint"] = "private-invalid-value"
        });
        builder.AddApiObservability();
        Assert.That(builder.Services.Any(x => x.ServiceType == typeof(TracerProvider)
            || x.ServiceType == typeof(MeterProvider) || x.ImplementationType == typeof(OpenTelemetryLoggerProvider)
            || x.ServiceType == typeof(TelemetrySettings)), Is.False);
    }

    [TestCase("")]
    [TestCase("private-invalid-value")]
    public void InvalidActivationFailsWithoutRepeatingValue(string value)
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        { ["Telemetry:Enabled"] = value });
        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(config));
        Assert.That(error!.Message, Is.EqualTo("Configuração de ativação da telemetria inválida."));
        Assert.That(error.InnerException, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("private-invalid-value")]
    [TestCase("file:///private")]
    [TestCase("http://user:private@localhost:4318")]
    [TestCase("http://localhost:4318?password=private")]
    [TestCase("http://localhost:4318#private")]
    public void InvalidEndpointFailsWithFixedMessage(string? endpoint)
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        { ["Telemetry:Enabled"] = "true", ["Telemetry:OtlpEndpoint"] = endpoint });
        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(config));
        Assert.That(error!.Message, Is.EqualTo("Endpoint de telemetria inválido ou ausente."));
        Assert.That(error.InnerException, Is.Null);
    }

    [Test]
    public void StandardEndpointTakesPrecedenceOverLegacySetting()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true", ["Telemetry:OtlpEndpoint"] = "private-invalid-legacy-value",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "https://localhost:4318"
        });
        Assert.That(TelemetrySettings.Read(config).Endpoint!.AbsoluteUri, Is.EqualTo("https://localhost:4318/"));
    }

    [Test]
    public void InvalidStandardEndpointDoesNotFallBackToLegacySetting()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true", ["Telemetry:OtlpEndpoint"] = "http://localhost:4318",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "https://user:private@localhost:4318"
        });
        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(config));
        Assert.That(error!.Message, Is.EqualTo("Endpoint de telemetria inválido ou ausente."));
    }

    [TestCase("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT")]
    [TestCase("OTEL_EXPORTER_OTLP_LOGS_PROTOCOL")]
    [TestCase("OTEL_EXPORTER_OTLP_HEADERS")]
    [TestCase("OTEL_EXPORTER_OTLP_METRICS_HEADERS")]
    public void RejectsOverridesThatBypassTheSingleUnauthenticatedHttpEndpoint(string key)
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true", ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318",
            [key] = "private-configuration-value"
        });
        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(config));
        Assert.That(error!.Message, Does.Not.Contain("private-configuration-value"));
        Assert.That(error.InnerException, Is.Null);
    }

    [TestCase(null, "local")]
    [TestCase("hom", "hom")]
    [TestCase("prd", "prd")]
    public void ReadsExplicitEndpointAndRuntimeIdentity(string? environment, string expected)
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true", ["Telemetry:OtlpEndpoint"] = "http://localhost:4318",
            ["Runtime:Environment"] = environment
        });
        var settings = TelemetrySettings.Read(config);
        Assert.Multiple(() =>
        {
            Assert.That(settings.Enabled, Is.True);
            Assert.That(settings.Environment, Is.EqualTo(expected));
            Assert.That(settings.Endpoint!.Host, Is.EqualTo("localhost"));
            Assert.That(settings.Version, Does.Match("^([a-f0-9]{40}|local)$"));
            Assert.That(TelemetrySettings.ReadVersion(typeof(string).Assembly), Is.EqualTo("local").Or.Match("^[a-f0-9]{40}$"));
        });
    }
    [Test]
    public void EnabledWithoutCollectionConfigurationFailsClosed()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Telemetry:Enabled"] = "true", ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318" }).Build();
        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(config));
        Assert.That(error!.Message, Is.EqualTo("Configuração de coleta de telemetria ausente."));
    }

    [Test]
    public void IncompleteCollectionConfigurationFailsWithFixedMessage()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true", ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318",
            ["Telemetry:Collection:HttpErrorMessage"] = ""
        });
        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(config));
        Assert.That(error!.Message, Is.EqualTo("Configuração de coleta de telemetria incompleta."));
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(TestContext.CurrentContext.TestDirectory, "Observability", "observability.json"))
            .AddInMemoryCollection(values)
            .Build();

}
