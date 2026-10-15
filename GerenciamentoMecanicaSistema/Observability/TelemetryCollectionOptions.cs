namespace GerenciamentoMecanicaSistema.Observability;

public sealed class TelemetryCollectionOptions
{
    public HashSet<string> ExcludedPaths { get; init; } = [];
    public HashSet<string> HttpMethods { get; init; } = [];
    public HashSet<string> HttpTraceAttributes { get; init; } = [];
    public HashSet<string> DatabaseTraceAttributes { get; init; } = [];
    public HashSet<string> ExceptionAttributes { get; init; } = [];
    public string[] HttpMetricAttributes { get; init; } = [];
    public string HttpErrorMessage { get; init; } = "";
    public string DatabaseErrorMessage { get; init; } = "";
    public string DatabaseSpanName { get; init; } = "";

    public void Validate()
    {
        IEnumerable<string>[] lists =
        [ExcludedPaths, HttpMethods, HttpTraceAttributes, DatabaseTraceAttributes, ExceptionAttributes, HttpMetricAttributes];
        if (lists.Any(values => values is null || !values.Any() || values.Any(string.IsNullOrWhiteSpace))
            || string.IsNullOrWhiteSpace(HttpErrorMessage) || string.IsNullOrWhiteSpace(DatabaseErrorMessage)
            || string.IsNullOrWhiteSpace(DatabaseSpanName))
            throw new InvalidOperationException("Configuração de coleta de telemetria incompleta.");
    }
}
