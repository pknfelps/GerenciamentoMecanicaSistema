using GerenciamentoMecanicaSistema.Observability;
using Service.Interface.Observability;

namespace ControllerTests.Observability;

public sealed class OrderDurationMeasurementsTests
{
    private static readonly DateTime OpenedAt = new(2026, 11, 1, 23, 50, 0, DateTimeKind.Utc);

    [Test]
    public void DiagnosisUsesPersistedUtcTimestampsEvenWhenCrossingMidnight()
    {
        using var recorder = new BusinessMetricRecorder();
        var history = ApprovedOrderHistory()[..3];

        OrderDurationMeasurements.Record(history, "WaitingForApproval", recorder.Metrics);

        var measurement = recorder.Measurements.Single();
        Assert.Multiple(() =>
        {
            Assert.That(measurement.Name, Is.EqualTo("mecanica.orders.stage.duration"));
            Assert.That(measurement.Value, Is.EqualTo(15 * 60));
            Assert.That(measurement.Tags, Is.EquivalentTo(new Dictionary<string, object?> { ["stage"] = "diagnosis" }));
        });
    }

    [Test]
    public void ServiceCompletionRecordsExecutionAndTotalServiceSeparately()
    {
        using var recorder = new BusinessMetricRecorder();
        var history = ApprovedOrderHistory()[..6];

        OrderDurationMeasurements.Record(history, "Finished", recorder.Metrics);

        Assert.That(recorder.Measurements, Has.Count.EqualTo(2));
        var execution = recorder.Measurements[0];
        var service = recorder.Measurements[1];
        Assert.Multiple(() =>
        {
            Assert.That(execution.Name, Is.EqualTo("mecanica.orders.stage.duration"));
            Assert.That(execution.Value, Is.EqualTo(60 * 60));
            Assert.That(execution.Tags, Is.EquivalentTo(new Dictionary<string, object?> { ["stage"] = "execution" }));
            Assert.That(service.Name, Is.EqualTo("mecanica.orders.service.duration"));
            Assert.That(service.Value, Is.EqualTo(90 * 60));
            Assert.That(service.Tags, Is.Empty);
        });
    }

    [Test]
    public void DeliveryAfterServiceCompletionRecordsOnlyTheDeliveryWait()
    {
        using var recorder = new BusinessMetricRecorder();

        OrderDurationMeasurements.Record(ApprovedOrderHistory(), "Delivered", recorder.Metrics);

        var measurement = recorder.Measurements.Single();
        Assert.Multiple(() =>
        {
            Assert.That(measurement.Name, Is.EqualTo("mecanica.orders.stage.duration"));
            Assert.That(measurement.Value, Is.EqualTo(30 * 60));
            Assert.That(measurement.Tags, Is.EquivalentTo(new Dictionary<string, object?>
            {
                ["stage"] = "delivery_wait",
                ["completion.reason"] = "service_completed"
            }));
        });
    }

    [Test]
    public void DeliveryAfterBudgetRejectionHasItsOwnCompletionReason()
    {
        using var recorder = new BusinessMetricRecorder();

        OrderDurationMeasurements.Record(RejectedOrderHistory(), "Delivered", recorder.Metrics);

        var measurement = recorder.Measurements.Single();
        Assert.Multiple(() =>
        {
            Assert.That(measurement.Name, Is.EqualTo("mecanica.orders.stage.duration"));
            Assert.That(measurement.Value, Is.EqualTo(30 * 60));
            Assert.That(measurement.Tags, Is.EquivalentTo(new Dictionary<string, object?>
            {
                ["stage"] = "delivery_wait",
                ["completion.reason"] = "budget_rejected"
            }));
        });
    }

    [Test]
    public void EqualValidTimestampsProduceAZeroDurationSample()
    {
        using var recorder = new BusinessMetricRecorder();
        OrderHistoryEntry[] history =
        [
            Entry(1, null, "Received", OpenedAt),
            Entry(2, "Received", "InDiagnosis", OpenedAt),
            Entry(3, "InDiagnosis", "WaitingForApproval", OpenedAt)
        ];

        OrderDurationMeasurements.Record(history, "WaitingForApproval", recorder.Metrics);

        Assert.That(recorder.Measurements.Single().Value, Is.Zero);
    }

    [Test]
    public void MissingHistoryDoesNotBecomeAZeroDurationSample()
    {
        using var recorder = new BusinessMetricRecorder();

        Assert.Throws<InvalidOperationException>(() => OrderDurationMeasurements.Record([], "WaitingForApproval", recorder.Metrics));

        Assert.That(recorder.Measurements, Is.Empty);
    }

    [Test]
    public void OpenDiagnosisDoesNotEmitACompletedInterval()
    {
        using var recorder = new BusinessMetricRecorder();
        var history = ApprovedOrderHistory()[..2];

        Assert.Throws<InvalidOperationException>(() => OrderDurationMeasurements.Record(history, "WaitingForApproval", recorder.Metrics));

        Assert.That(recorder.Measurements, Is.Empty);
    }

    [Test]
    public void RejectedBudgetDoesNotCountAsExecutionOrTotalService()
    {
        using var recorder = new BusinessMetricRecorder();
        var history = RejectedOrderHistory()[..4];

        Assert.Throws<InvalidOperationException>(() => OrderDurationMeasurements.Record(history, "Finished", recorder.Metrics));

        Assert.That(recorder.Measurements, Is.Empty);
    }

    [Test]
    public void BrokenStatusSequenceDoesNotEmitMeasurements()
    {
        using var recorder = new BusinessMetricRecorder();
        var history = ApprovedOrderHistory()[..3];
        history[2] = Entry(3, "Received", "WaitingForApproval", OpenedAt.AddMinutes(20));

        Assert.Throws<InvalidOperationException>(() => OrderDurationMeasurements.Record(history, "WaitingForApproval", recorder.Metrics));

        Assert.That(recorder.Measurements, Is.Empty);
    }

    [Test]
    public void BackwardsTimestampsDoNotEmitNegativeDurations()
    {
        using var recorder = new BusinessMetricRecorder();
        var history = ApprovedOrderHistory()[..3];
        history[2] = Entry(3, "InDiagnosis", "WaitingForApproval", OpenedAt.AddMinutes(4));

        Assert.Throws<InvalidOperationException>(() => OrderDurationMeasurements.Record(history, "WaitingForApproval", recorder.Metrics));

        Assert.That(recorder.Measurements, Is.Empty);
    }

    [Test]
    public void IncompatibleCompletionReasonDoesNotEmitMeasurements()
    {
        using var recorder = new BusinessMetricRecorder();
        var history = ApprovedOrderHistory()[..6];
        history[5] = Entry(6, "InExecution", "Finished", OpenedAt.AddMinutes(90), "BudgetRejected");

        Assert.Throws<InvalidOperationException>(() => OrderDurationMeasurements.Record(history, "Finished", recorder.Metrics));

        Assert.That(recorder.Measurements, Is.Empty);
    }

    [TestCase(DateTimeKind.Unspecified)]
    [TestCase(DateTimeKind.Local)]
    public void TimestampsWithoutUtcKindAreRejected(DateTimeKind kind)
    {
        using var recorder = new BusinessMetricRecorder();
        var history = ApprovedOrderHistory()[..3];
        history[2] = Entry(3, "InDiagnosis", "WaitingForApproval", DateTime.SpecifyKind(OpenedAt.AddMinutes(20), kind));

        Assert.Throws<InvalidOperationException>(() => OrderDurationMeasurements.Record(history, "WaitingForApproval", recorder.Metrics));

        Assert.That(recorder.Measurements, Is.Empty);
    }

    private static OrderHistoryEntry[] ApprovedOrderHistory() =>
    [
        Entry(1, null, "Received", OpenedAt),
        Entry(2, "Received", "InDiagnosis", OpenedAt.AddMinutes(5)),
        Entry(3, "InDiagnosis", "WaitingForApproval", OpenedAt.AddMinutes(20)),
        Entry(4, "WaitingForApproval", "WaitingForExecution", OpenedAt.AddMinutes(25)),
        Entry(5, "WaitingForExecution", "InExecution", OpenedAt.AddMinutes(30)),
        Entry(6, "InExecution", "Finished", OpenedAt.AddMinutes(90), "ServiceCompleted"),
        Entry(7, "Finished", "Delivered", OpenedAt.AddMinutes(120))
    ];

    private static OrderHistoryEntry[] RejectedOrderHistory() =>
    [
        Entry(1, null, "Received", OpenedAt),
        Entry(2, "Received", "InDiagnosis", OpenedAt.AddMinutes(5)),
        Entry(3, "InDiagnosis", "WaitingForApproval", OpenedAt.AddMinutes(20)),
        Entry(4, "WaitingForApproval", "Finished", OpenedAt.AddMinutes(25), "BudgetRejected"),
        Entry(5, "Finished", "Delivered", OpenedAt.AddMinutes(55))
    ];

    private static OrderHistoryEntry Entry(int sequence, string? previousStatus, string newStatus, DateTime occurredAt, string? reason = null) => new()
    {
        Sequence = sequence,
        PreviousStatus = previousStatus,
        NewStatus = newStatus,
        OccurredAt = occurredAt,
        Reason = reason
    };
}
