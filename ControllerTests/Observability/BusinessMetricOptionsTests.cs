using GerenciamentoMecanicaSistema.Observability;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

namespace ControllerTests.Observability;

public sealed class BusinessMetricOptionsTests
{
    private static readonly string[] AllowedAttributes = ["stage", "outcome"];
    private static readonly double[] DurationBoundaries = [1, 5, 60];
    private readonly BusinessMetricOptions options = new()
    {
        Attributes = AllowedAttributes,
        DurationBoundariesSeconds = DurationBoundaries
    };

    [Test]
    public void DoesNotOverrideMetricsFromOtherMeters()
    {
        using var meter = new Meter("Other.Meter");
        var counter = meter.CreateCounter<long>("other.counter");

        var view = options.CreateView(counter);

        Assert.That(view, Is.Null);
    }

    [Test]
    public void RestrictsBusinessCountersToConfiguredAttributes()
    {
        using var meter = new Meter(OrderBusinessMetrics.MeterName);
        var counter = meter.CreateCounter<long>("mecanica.orders.created");

        var view = options.CreateView(counter);

        Assert.Multiple(() =>
        {
            Assert.That(view, Is.TypeOf<MetricStreamConfiguration>());
            Assert.That(view!.TagKeys, Is.EqualTo(AllowedAttributes));
        });
    }

    [Test]
    public void UsesConfiguredAttributesAndBucketsForBusinessDurations()
    {
        using var meter = new Meter(OrderBusinessMetrics.MeterName);
        var histogram = meter.CreateHistogram<double>("mecanica.orders.stage.duration", "s");

        var view = options.CreateView(histogram);

        Assert.That(view, Is.TypeOf<ExplicitBucketHistogramConfiguration>());
        var histogramView = (ExplicitBucketHistogramConfiguration)view!;
        Assert.Multiple(() =>
        {
            Assert.That(histogramView.TagKeys, Is.EqualTo(AllowedAttributes));
            Assert.That(histogramView.Boundaries, Is.EqualTo(DurationBoundaries));
        });
    }
}
