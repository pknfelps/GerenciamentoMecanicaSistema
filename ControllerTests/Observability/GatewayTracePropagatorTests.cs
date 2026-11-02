using GerenciamentoMecanicaSistema.Observability;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace ControllerTests.Observability;

public sealed class GatewayTracePropagatorTests
{
    private const string XRayHeader = "Root=1-6ad3c000-000000000000000000000001;Parent=0000000000000101;Sampled=1";
    private const string W3CHeader = "00-00000000000000000000000000000002-0000000000000202-01";
    private const string W3CTraceId = "00000000000000000000000000000002";
    private const string W3CParentId = "0000000000000202";
    private static readonly string[] OutboundHeaderNames = ["traceparent"];

    [Test]
    public void XRayHeaderAloneDoesNotSelectAParent()
    {
        var context = Extract(new GatewayTracePropagator(),
            new() { ["X-Amzn-Trace-Id"] = [XRayHeader] });

        Assert.That(context.ActivityContext, Is.EqualTo(default(ActivityContext)));
    }

    [Test]
    public void ReadsTraceAndParentFromW3CHeader()
    {
        var context = Extract(new GatewayTracePropagator(),
            new() { ["traceparent"] = [W3CHeader] });

        AssertParent(context, W3CTraceId, W3CParentId);
    }

    [Test]
    public void UsesW3CWhenBothHeadersAreValid()
    {
        var context = Extract(new GatewayTracePropagator(),
            new() { ["X-Amzn-Trace-Id"] = [XRayHeader], ["traceparent"] = [W3CHeader] });

        AssertParent(context, W3CTraceId, W3CParentId);
    }

    [Test]
    public void InvalidW3CDoesNotFallBackToXRay()
    {
        var context = Extract(new GatewayTracePropagator(),
            new() { ["X-Amzn-Trace-Id"] = [XRayHeader], ["traceparent"] = ["invalid-w3c"] });

        Assert.That(context.ActivityContext, Is.EqualTo(default(ActivityContext)));
    }

    [Test]
    public void InvalidXRayDoesNotInterfereWithW3C()
    {
        var context = Extract(new GatewayTracePropagator(),
            new() { ["X-Amzn-Trace-Id"] = ["invalid-xray"], ["traceparent"] = [W3CHeader] });

        AssertParent(context, W3CTraceId, W3CParentId);
    }

    [Test]
    public void MultipleXRayValuesDoNotInterfereWithW3C()
    {
        var context = Extract(new GatewayTracePropagator(),
            new() { ["X-Amzn-Trace-Id"] = [XRayHeader, XRayHeader], ["traceparent"] = [W3CHeader] });

        AssertParent(context, W3CTraceId, W3CParentId);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReturnsNoParentWhenHeadersAreMissingOrInvalid(bool includeInvalidHeaders)
    {
        var headers = new Dictionary<string, string[]>();
        if (includeInvalidHeaders)
        {
            headers["X-Amzn-Trace-Id"] = ["invalid-xray"];
            headers["traceparent"] = ["invalid-w3c"];
        }

        var context = Extract(new GatewayTracePropagator(), headers);

        Assert.That(context.ActivityContext, Is.EqualTo(default(ActivityContext)));
    }

    [Test]
    public void PreservesContextFromAnUnsampledW3CParent()
    {
        var context = Extract(new GatewayTracePropagator(), new()
        {
            ["traceparent"] = ["00-00000000000000000000000000000002-0000000000000202-00"]
        });

        Assert.Multiple(() =>
        {
            Assert.That(context.ActivityContext.TraceId.ToString(), Is.EqualTo(W3CTraceId));
            Assert.That(context.ActivityContext.TraceFlags, Is.EqualTo(ActivityTraceFlags.None));
        });
    }

    [Test]
    public void DoesNotReadOrReturnBaggageAndTraceState()
    {
        var headers = new Dictionary<string, string[]>
        {
            ["traceparent"] = [W3CHeader],
            ["tracestate"] = ["vendor=private-state"],
            ["baggage"] = ["document=private-document"]
        };
        var requestedHeaders = new List<string>();
        var propagator = new GatewayTracePropagator();

        var context = propagator.Extract(default, headers, (input, key) =>
        {
            requestedHeaders.Add(key);
            return input.GetValueOrDefault(key, []);
        });

        Assert.Multiple(() =>
        {
            Assert.That(context.ActivityContext.TraceId.ToString(), Is.EqualTo(W3CTraceId));
            Assert.That(context.ActivityContext.TraceState, Is.Null.Or.Empty);
            Assert.That(context.Baggage.GetBaggage(), Is.Empty);
            Assert.That(requestedHeaders, Does.Not.Contain("tracestate").And.Not.Contain("baggage"));
        });
    }

    [Test]
    public void InjectsOnlyW3CWithoutMetadata()
    {
        var activity = new ActivityContext(ActivityTraceId.CreateFromString(W3CTraceId),
            ActivitySpanId.CreateFromString(W3CParentId), ActivityTraceFlags.Recorded, "vendor=private-state", isRemote: true);
        var context = new PropagationContext(activity,
            Baggage.Create(new Dictionary<string, string> { ["document"] = "private-document" }));
        var headers = new Dictionary<string, string>();
        var propagator = new GatewayTracePropagator();

        propagator.Inject(context, headers, static (output, key, value) => output[key] = value);

        Assert.Multiple(() =>
        {
            Assert.That(headers.Keys, Is.EquivalentTo(OutboundHeaderNames));
            Assert.That(headers["traceparent"], Is.EqualTo(W3CHeader));
        });
    }

    [Test]
    public void DoesNotReuseParentFromThePreviousRequest()
    {
        var propagator = new GatewayTracePropagator();

        var first = Extract(propagator, new() { ["traceparent"] = [W3CHeader] });
        var second = Extract(propagator, new() { ["X-Amzn-Trace-Id"] = [XRayHeader] });
        var withoutHeaders = Extract(propagator, new());

        AssertParent(first, W3CTraceId, W3CParentId);
        Assert.That(second.ActivityContext, Is.EqualTo(default(ActivityContext)));
        Assert.That(withoutHeaders.ActivityContext, Is.EqualTo(default(ActivityContext)));
    }

    [Test]
    public void DeclaresOnlyTraceParentInFields()
    {
        var propagator = new GatewayTracePropagator();

        Assert.That(propagator.Fields, Is.EquivalentTo(OutboundHeaderNames).IgnoreCase);
    }

    [Test]
    public void AmbiguousW3CDoesNotFallBackToXRay()
    {
        var context = Extract(new GatewayTracePropagator(), new()
        {
            ["traceparent"] = [W3CHeader, W3CHeader],
            ["X-Amzn-Trace-Id"] = [XRayHeader]
        });

        Assert.That(context.ActivityContext, Is.EqualTo(default(ActivityContext)));
    }

    private static PropagationContext Extract(GatewayTracePropagator propagator, Dictionary<string, string[]> headers) =>
        propagator.Extract(default, headers, static (input, key) => input.GetValueOrDefault(key, []));

    private static void AssertParent(PropagationContext context, string traceId, string parentId)
    {
        Assert.Multiple(() =>
        {
            Assert.That(context.ActivityContext.TraceId.ToString(), Is.EqualTo(traceId));
            Assert.That(context.ActivityContext.SpanId.ToString(), Is.EqualTo(parentId));
            Assert.That(context.ActivityContext.IsRemote, Is.True);
        });
    }
}
