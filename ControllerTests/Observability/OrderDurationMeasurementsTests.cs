using GerenciamentoMecanicaSistema.Observability;
using Microsoft.Extensions.DependencyInjection;
using Service.Interface.Observability;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class OrderDurationMeasurementsTests : BaseControllerTests
{
    private TelemetryWebApplicationFactory Telemetry => (TelemetryWebApplicationFactory)TestWebAppFactory;
    protected override TestWebApplicationFactory CreateFactory() => new TelemetryWebApplicationFactory
    { TelemetryEnabled = "true", TelemetryEndpoint = "http://127.0.0.1:1" };
    protected override void MockService() { }

    public static IReadOnlyList<OrderHistoryEntry> History(bool approved = true)
    {
        var opening = new DateTime(2026, 10, 13, 2, 59, 0, DateTimeKind.Utc);
        var states = approved
            ? new[] { "Received", "InDiagnosis", "WaitingForApproval", "WaitingForExecution", "InExecution", "Finished", "Delivered" }
            : new[] { "Received", "InDiagnosis", "WaitingForApproval", "Finished", "Delivered" };
        return states.Select((status, index) => new OrderHistoryEntry
        {
            Sequence = index + 1, PreviousStatus = index == 0 ? null : states[index - 1], NewStatus = status,
            OccurredAt = opening.AddDays(index),
            Reason = (status, approved) switch
            { ("Finished", true) => "ServiceCompleted", ("Finished", false) => "BudgetRejected", _ => null }
        }).ToArray();
    }

    [TestCase("WaitingForApproval", "diagnosis", true)]
    [TestCase("Finished", "execution", true)]
    [TestCase("Delivered", "delivery_wait", true)]
    [TestCase("Delivered", "delivery_wait", false)]
    public void PersistedInstantsDetermineCompletedIntervalsEvenWithLaterEvents(string completed, string stage, bool approved)
    {
        var history = History(approved);
        OrderDurationMeasurements.Record(history, completed, Telemetry.Services.GetRequiredService<OrderBusinessMetrics>());
        Telemetry.Flush();
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.stage.duration", ("stage", stage)), Is.EqualTo(1));
        Assert.That(BusinessMetricAssertions.Sum(Telemetry.Metrics, "mecanica.orders.stage.duration"), Is.EqualTo(86400));
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.service.duration"), Is.EqualTo(completed == "Finished" ? 1 : 0));
        if (completed == "Finished")
            Assert.That(BusinessMetricAssertions.Sum(Telemetry.Metrics, "mecanica.orders.service.duration"), Is.EqualTo(5 * 86400));
        if (stage == "delivery_wait")
            Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.stage.duration",
                ("completion.reason", approved ? "service_completed" : "budget_rejected")), Is.EqualTo(1));
    }

    [Test]
    public void EqualValidInstantsProduceZeroInsteadOfAnInventedMissingSample()
    {
        var history = History().Select(entry => Copy(entry, occurredAt: History()[0].OccurredAt)).ToArray();
        OrderDurationMeasurements.Record(history, "Finished", Telemetry.Services.GetRequiredService<OrderBusinessMetrics>());
        Telemetry.Flush();
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.service.duration"), Is.EqualTo(1));
        Assert.That(BusinessMetricAssertions.Sum(Telemetry.Metrics, "mecanica.orders.service.duration"), Is.Zero);
    }

    [TestCase("empty")]
    [TestCase("sequence")]
    [TestCase("duplicate")]
    [TestCase("backwards")]
    [TestCase("unspecified")]
    [TestCase("reason")]
    [TestCase("chain")]
    [TestCase("unknown")]
    [TestCase("open")]
    [TestCase("rejected")]
    public void IncompatibleOrUncompletedHistoryDoesNotEmit(string scenario)
    {
        var history = History().ToArray();
        switch (scenario)
        {
            case "empty": history = []; break;
            case "sequence": history[2] = Copy(history[2], sequence: 4); break;
            case "duplicate": history[2] = Copy(history[2], sequence: 2); break;
            case "backwards": history[2] = Copy(history[2], occurredAt: history[0].OccurredAt.AddSeconds(-1)); break;
            case "unspecified": history[2] = Copy(history[2], occurredAt: DateTime.SpecifyKind(history[2].OccurredAt, DateTimeKind.Unspecified)); break;
            case "reason": history[5] = Copy(history[5], reason: "private-invalid-reason"); break;
            case "chain": history[2] = Copy(history[2], previous: "Received"); break;
            case "unknown": history[2] = Copy(history[2], status: "private-invalid-status"); break;
            case "open": history = history.Take(5).ToArray(); break;
            case "rejected": history = History(false).ToArray(); break;
        }
        Assert.Throws<InvalidOperationException>(() => OrderDurationMeasurements.Record(history, "Finished",
            Telemetry.Services.GetRequiredService<OrderBusinessMetrics>()));
        Telemetry.Flush();
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.stage.duration"), Is.Zero);
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.service.duration"), Is.Zero);
    }

    private static OrderHistoryEntry Copy(OrderHistoryEntry entry, int? sequence = null, string? previous = null,
        string? status = null, DateTime? occurredAt = null, string? reason = null) => new()
    {
        Sequence = sequence ?? entry.Sequence, PreviousStatus = previous ?? entry.PreviousStatus,
        NewStatus = status ?? entry.NewStatus, OccurredAt = occurredAt ?? entry.OccurredAt, Reason = reason ?? entry.Reason
    };
}
