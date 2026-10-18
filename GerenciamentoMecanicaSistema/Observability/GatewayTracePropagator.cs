using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Extensions.AWS.Trace;
using System.Diagnostics;

namespace GerenciamentoMecanicaSistema.Observability;

public sealed class GatewayTracePropagator : TextMapPropagator
{
    private readonly CompositeTextMapPropagator inbound;
    private readonly TraceContextPropagator outbound = new();
    private readonly HashSet<string> fields;

    public GatewayTracePropagator(IEnumerable<string> inboundFormats)
    {
        inbound = new CompositeTextMapPropagator(inboundFormats.Select(format => format switch
        {
            "xray" => (TextMapPropagator)new AWSXRayPropagator(),
            "tracecontext" => new TraceContextPropagator(),
            _ => throw new InvalidOperationException("Configuração de propagação de telemetria inválida.")
        }));
        fields = inbound.Fields.Where(field => !string.Equals(field, "tracestate", StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        fields.Add("traceparent");
    }

    public override ISet<string> Fields => fields;

    public override PropagationContext Extract<T>(PropagationContext context, T carrier,
        Func<T, string, IEnumerable<string>?> getter)
    {
        var extracted = inbound.Extract(WithoutMetadata(context.ActivityContext), carrier,
            (input, name) => string.Equals(name, "tracestate", StringComparison.OrdinalIgnoreCase)
                ? []
                : getter(input, name));
        return WithoutMetadata(extracted.ActivityContext);
    }

    public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter) =>
        outbound.Inject(WithoutMetadata(context.ActivityContext), carrier, setter);

    private static PropagationContext WithoutMetadata(ActivityContext context) =>
        new(new ActivityContext(context.TraceId, context.SpanId, context.TraceFlags, isRemote: context.IsRemote), default);
}
