namespace GerenciamentoMecanicaSistema.Observability;

public sealed class BusinessMetricOptions
{
    public int HistoryQueryTimeoutSeconds { get; init; }
    public double[] DurationBoundariesSeconds { get; init; } = [];
    public string[] Attributes { get; init; } = [];

    public void Validate()
    {
        if (HistoryQueryTimeoutSeconds is < 1 or > 5 || DurationBoundariesSeconds is null || DurationBoundariesSeconds.Length == 0
            || DurationBoundariesSeconds.Any(value => !double.IsFinite(value) || value <= 0)
            || !DurationBoundariesSeconds.SequenceEqual(DurationBoundariesSeconds.Distinct().Order())
            || Attributes is null || Attributes.Length == 0 || Attributes.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Configuração das métricas de negócio inválida.");
    }
}
