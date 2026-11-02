using OpenTelemetry.Trace;

namespace GerenciamentoMecanicaSistema.Observability;

public sealed class ApiTelemetrySampler(IHttpContextAccessor accessor, TelemetryCollectionOptions collection) : Sampler
{
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters) =>
        new(TelemetryRequest.IsProbe(accessor.HttpContext, collection)
            ? SamplingDecision.Drop
            : SamplingDecision.RecordAndSample);
}
