using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace GerenciamentoMecanicaSistema.Observability;

public sealed class GatewayTracePropagator : TextMapPropagator
{
    private readonly TraceContextPropagator w3c = new();

    public override ISet<string> Fields { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "traceparent" };

    public override PropagationContext Extract<T>(PropagationContext context, T carrier,
        Func<T, string, IEnumerable<string>?> getter)
    {
        var extracted = w3c.Extract(WithoutMetadata(context.ActivityContext), carrier,
            (input, name) => string.Equals(name, "tracestate", StringComparison.OrdinalIgnoreCase)
                ? []
                : getter(input, name));
        return WithoutMetadata(extracted.ActivityContext);
    }

    public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter) =>
        w3c.Inject(WithoutMetadata(context.ActivityContext), carrier, setter);

    private static PropagationContext WithoutMetadata(ActivityContext context) =>
        new(new ActivityContext(context.TraceId, context.SpanId, context.TraceFlags, isRemote: context.IsRemote), default);
}
