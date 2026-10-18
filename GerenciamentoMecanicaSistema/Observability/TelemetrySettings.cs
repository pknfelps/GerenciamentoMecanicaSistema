using System.Reflection;

namespace GerenciamentoMecanicaSistema.Observability;

public sealed class TelemetrySettings
{
    private const string LocalEnvironment = "local";

    public bool Enabled { get; private init; }
    public Uri? Endpoint { get; private init; }
    public string Environment { get; private init; } = LocalEnvironment;
    public string Version { get; private init; } = LocalEnvironment;
    public string[] InboundFormats { get; private init; } = [];
    public TelemetryCollectionOptions Collection { get; private init; } = new();

    public static TelemetrySettings Read(IConfiguration configuration)
    {
        var value = configuration["Telemetry:Enabled"];
        if (value is null)
            return new();
        if (!bool.TryParse(value, out var enabled))
            throw new InvalidOperationException("Configuração de ativação da telemetria inválida.");
        if (!enabled)
            return new();

        foreach (var signal in new[] { "", "TRACES_", "METRICS_", "LOGS_" })
        {
            if (!string.IsNullOrEmpty(configuration[$"OTEL_EXPORTER_OTLP_{signal}HEADERS"])
                || (signal.Length > 0 && (configuration[$"OTEL_EXPORTER_OTLP_{signal}ENDPOINT"] is not null
                    || configuration[$"OTEL_EXPORTER_OTLP_{signal}PROTOCOL"] is not null)))
                throw new InvalidOperationException("Utilize o endpoint único de telemetria, sem cabeçalhos de autenticação ou protocolo por sinal.");
        }

        var endpointValue = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? configuration["Telemetry:OtlpEndpoint"];
        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new InvalidOperationException("Endpoint de telemetria inválido ou ausente.");

        var environment = configuration["Runtime:Environment"] ?? LocalEnvironment;
        if (environment is not (LocalEnvironment or "hom" or "prd"))
            throw new InvalidOperationException("Ambiente de telemetria inválido.");

        var collection = configuration.GetSection("Telemetry:Collection").Get<TelemetryCollectionOptions>()
            ?? throw new InvalidOperationException("Configuração de coleta de telemetria ausente.");
        collection.Validate();

        return new()
        {
            Enabled = true,
            Endpoint = endpoint,
            Environment = environment,
            Version = ReadVersion(typeof(Program).Assembly),
            InboundFormats = ReadInboundFormats(configuration),
            Collection = collection
        };
    }

    private static string[] ReadInboundFormats(IConfiguration configuration)
    {
        string[] inboundFormats;
        try
        {
            inboundFormats = configuration.GetSection("Telemetry:Propagation:InboundFormats").Get<string[]>() ?? [];
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("Configuração de propagação de telemetria inválida.");
        }
        if (inboundFormats.Length == 0
            || inboundFormats.Any(format => format is not ("xray" or "tracecontext"))
            || inboundFormats.Distinct(StringComparer.Ordinal).Count() != inboundFormats.Length)
            throw new InvalidOperationException("Configuração de propagação de telemetria inválida.");

        return inboundFormats;
    }

    public static string ReadVersion(Assembly assembly)
    {
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var metadata = informationalVersion?.Split('+', 2).ElementAtOrDefault(1);
        return metadata is { Length: >= 40 } && metadata[..40].All(Uri.IsHexDigit)
            ? metadata[..40].ToLowerInvariant()
            : LocalEnvironment;
    }
}
