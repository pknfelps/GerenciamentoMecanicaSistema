using System.Diagnostics.Metrics;

namespace ControllerTests.Observability;

public sealed class OrderBusinessMetricsTests
{
    [Test]
    public void EachPersistedCreationAddsOneWithoutOrderIdentifiers()
    {
        using var recorder = new BusinessMetricRecorder();

        recorder.Metrics.RecordCreated();
        recorder.Metrics.RecordCreated();

        Assert.Multiple(() =>
        {
            Assert.That(recorder.Measurements.Select(point => point.Name), Is.EqualTo(new[] { "mecanica.orders.created", "mecanica.orders.created" }));
            Assert.That(recorder.Measurements.Select(point => point.Value), Is.EqualTo(new[] { 1d, 1d }));
            Assert.That(recorder.Measurements.All(point => point.Tags.Count == 0), Is.True);
        });
    }

    [TestCase(true, true, "budget_available", "success")]
    [TestCase(true, false, "budget_available", "failure")]
    [TestCase(false, true, "status_changed", "success")]
    [TestCase(false, false, "status_changed", "failure")]
    public void NotificationRecordsOneAttemptWithOnlyTypeOutcomeAndMode(bool budget, bool succeeded, string type, string outcome)
    {
        using var recorder = new BusinessMetricRecorder();

        recorder.Metrics.RecordNotification(budget, succeeded);

        var measurement = recorder.Measurements.Single();
        Assert.Multiple(() =>
        {
            Assert.That(measurement.Name, Is.EqualTo("mecanica.orders.notifications"));
            Assert.That(measurement.Value, Is.EqualTo(1));
            Assert.That(measurement.Tags, Is.EquivalentTo(new Dictionary<string, object?>
            {
                ["notification.type"] = type,
                ["outcome"] = outcome,
                ["mode"] = "smtp"
            }));
        });
    }

    [Test]
    public void ProcessingFailureIdentifiesOnlyTheOperation()
    {
        using var recorder = new BusinessMetricRecorder();

        recorder.Metrics.RecordProcessingFailure("create");

        var measurement = recorder.Measurements.Single();
        Assert.Multiple(() =>
        {
            Assert.That(measurement.Name, Is.EqualTo("mecanica.orders.processing.failures"));
            Assert.That(measurement.Value, Is.EqualTo(1));
            Assert.That(measurement.Tags, Is.EquivalentTo(new Dictionary<string, object?> { ["operation"] = "create" }));
        });
    }

    [Test]
    public void MeasurementFailureHasItsOwnCounterWithoutProcessingFailure()
    {
        using var recorder = new BusinessMetricRecorder();

        recorder.Metrics.RecordTelemetryFailure();

        var measurement = recorder.Measurements.Single();
        Assert.Multiple(() =>
        {
            Assert.That(measurement.Name, Is.EqualTo("mecanica.orders.telemetry.failures"));
            Assert.That(measurement.Value, Is.EqualTo(1));
            Assert.That(measurement.Tags, Is.Empty);
        });
    }

    [Test]
    public void FailingMetricListenerDoesNotInterruptTheOperation()
    {
        using var recorder = new BusinessMetricRecorder();
        using var failingListener = new MeterListener();
        var listenerWasCalled = false;
        failingListener.InstrumentPublished = (instrument, owner) =>
        {
            if (ReferenceEquals(instrument.Meter, recorder.Meter))
                owner.EnableMeasurementEvents(instrument);
        };
        failingListener.SetMeasurementEventCallback<long>((_, _, _, _) =>
        {
            listenerWasCalled = true;
            throw new InvalidOperationException("listener-failed");
        });
        failingListener.Start();

        Assert.DoesNotThrow(() => recorder.Metrics.RecordCreated());
        Assert.That(listenerWasCalled, Is.True);
    }
}
