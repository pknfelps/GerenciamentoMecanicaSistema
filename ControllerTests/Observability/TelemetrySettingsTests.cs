using GerenciamentoMecanicaSistema.Observability;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;

namespace ControllerTests.Observability;

public sealed class TelemetrySettingsTests
{
    private const string PropagationError = "Configuração de propagação de telemetria inválida.";
    private static readonly string[] ExpectedInboundFormats = ["xray", "tracecontext"];

    [TestCase(null)]
    [TestCase("false")]
    public void DisabledTelemetryDoesNotRequireEndpointOrPropagation(string? enabled)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = enabled,
            ["Telemetry:Propagation:InboundFormats"] = "private-invalid-value"
        }).Build();

        var settings = TelemetrySettings.Read(configuration);

        Assert.Multiple(() =>
        {
            Assert.That(settings.Enabled, Is.False);
            Assert.That(settings.Endpoint, Is.Null);
            Assert.That(settings.InboundFormats, Is.Empty);
        });
    }

    [Test]
    public void RejectsInvalidActivationWithFixedMessage()
    {
        var configuration = CreateConfiguration();
        configuration["Telemetry:Enabled"] = "private-invalid-value";

        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(configuration));

        Assert.That(error!.Message, Is.EqualTo("Configuração de ativação da telemetria inválida."));
    }

    [TestCase(null, "local")]
    [TestCase("local", "local")]
    [TestCase("hom", "hom")]
    [TestCase("prd", "prd")]
    public void LoadsEndpointEnvironmentAndConfiguredPropagation(string? environment, string expectedEnvironment)
    {
        var configuration = CreateConfiguration();
        configuration["Runtime:Environment"] = environment;

        var settings = TelemetrySettings.Read(configuration);

        Assert.Multiple(() =>
        {
            Assert.That(settings.Enabled, Is.True);
            Assert.That(settings.Endpoint, Is.EqualTo(new Uri("http://localhost:4318")));
            Assert.That(settings.Environment, Is.EqualTo(expectedEnvironment));
            Assert.That(settings.InboundFormats, Is.EqualTo(ExpectedInboundFormats));
            Assert.That(settings.Collection.ExcludedPaths, Does.Contain("/health/ready"));
        });
    }

    [TestCase("[\"tracecontext\",\"xray\"]", new[] { "tracecontext", "xray" })]
    [TestCase("[\"xray\"]", new[] { "xray" })]
    [TestCase("[\"tracecontext\"]", new[] { "tracecontext" })]
    public void PreservesTheOrderAndFormatsDeclaredInTheManifest(string formats, string[] expected)
    {
        var configuration = CreateConfiguration(formats);

        var settings = TelemetrySettings.Read(configuration);

        Assert.That(settings.InboundFormats, Is.EqualTo(expected));
    }

    [TestCase(null, TestName = "RejectsMissingPropagationFormats")]
    [TestCase("[]", TestName = "RejectsEmptyPropagationFormats")]
    [TestCase("[\"xray\",\"xray\"]", TestName = "RejectsDuplicatePropagationFormats")]
    [TestCase("[\"private-format\"]", TestName = "RejectsUnknownPropagationFormat")]
    [TestCase("[\"XRAY\"]", TestName = "RejectsIncorrectlyCasedPropagationFormat")]
    [TestCase("\"private-value\"", TestName = "RejectsScalarPropagationFormats")]
    public void RejectsInvalidPropagationWithFixedMessage(string? formats)
    {
        var configuration = CreateConfiguration(formats);

        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(configuration));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Is.EqualTo(PropagationError));
            Assert.That(error.InnerException, Is.Null);
        });
    }

    [TestCase(null)]
    [TestCase("not-an-endpoint")]
    [TestCase("ftp://localhost")]
    [TestCase("https://user:private-password@example.invalid")]
    [TestCase("https://example.invalid?key=private-key")]
    [TestCase("https://example.invalid#private-fragment")]
    public void RejectsMissingInvalidOrSensitiveEndpoint(string? endpoint)
    {
        var configuration = CreateConfiguration();
        configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = endpoint;

        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(configuration));

        Assert.That(error!.Message, Is.EqualTo("Endpoint de telemetria inválido ou ausente."));
    }

    [Test]
    public void PrefersStandardEndpointOverLegacySetting()
    {
        var configuration = CreateConfiguration();
        configuration["Telemetry:OtlpEndpoint"] = "http://legacy.invalid:4318";

        var settings = TelemetrySettings.Read(configuration);

        Assert.That(settings.Endpoint, Is.EqualTo(new Uri("http://localhost:4318")));
    }

    [Test]
    public void AcceptsLegacyEndpointWhenStandardSettingIsMissing()
    {
        var configuration = CreateConfiguration();
        configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = null;
        configuration["Telemetry:OtlpEndpoint"] = "https://collector.example.invalid";

        var settings = TelemetrySettings.Read(configuration);

        Assert.That(settings.Endpoint, Is.EqualTo(new Uri("https://collector.example.invalid")));
    }

    [TestCase("OTEL_EXPORTER_OTLP_HEADERS", "api-key=private-key")]
    [TestCase("OTEL_EXPORTER_OTLP_TRACES_HEADERS", "api-key=private-key")]
    [TestCase("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", "https://other.example.invalid")]
    [TestCase("OTEL_EXPORTER_OTLP_LOGS_PROTOCOL", "grpc")]
    public void RejectsAuthenticationHeadersAndPerSignalDestinations(string key, string value)
    {
        var configuration = CreateConfiguration();
        configuration[key] = value;

        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(configuration));

        Assert.That(error!.Message, Is.EqualTo("Utilize o endpoint único de telemetria, sem cabeçalhos de autenticação ou protocolo por sinal."));
    }

    [Test]
    public void RejectsUnknownEnvironmentWithoutIncludingItsValue()
    {
        var configuration = CreateConfiguration();
        configuration["Runtime:Environment"] = "private-environment";

        var error = Assert.Throws<InvalidOperationException>(() => TelemetrySettings.Read(configuration));

        Assert.That(error!.Message, Is.EqualTo("Ambiente de telemetria inválido."));
    }

    [TestCase(null, "local")]
    [TestCase("1.0.0", "local")]
    [TestCase("1.0.0+private-build", "local")]
    [TestCase("1.0.0+ABCDEF0123456789ABCDEF0123456789ABCDEF01", "abcdef0123456789abcdef0123456789abcdef01")]
    public void UsesCommitAsServiceVersionOrFallsBackToLocal(string? informationalVersion, string expected)
    {
        var assembly = Substitute.For<Assembly>();
        Attribute[] attributes = informationalVersion is null ? [] : [new AssemblyInformationalVersionAttribute(informationalVersion)];
        assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), Arg.Any<bool>())
            .Returns(attributes);

        var version = TelemetrySettings.ReadVersion(assembly);

        Assert.That(version, Is.EqualTo(expected));
    }

    // Start from the versioned manifest; each test changes only the setting it exercises.
    private static IConfigurationRoot CreateConfiguration(string? formats = "[\"xray\",\"tracecontext\"]")
    {
        var manifestPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Observability", "observability.json");
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!;
        manifest["Telemetry"]!["Enabled"] = true;
        manifest["Telemetry"]!["Propagation"]!["InboundFormats"] = formats is null ? null : JsonNode.Parse(formats);
        var json = new MemoryStream(Encoding.UTF8.GetBytes(manifest.ToJsonString()));

        return new ConfigurationBuilder().AddJsonStream(json)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318" })
            .Build();
    }
}
