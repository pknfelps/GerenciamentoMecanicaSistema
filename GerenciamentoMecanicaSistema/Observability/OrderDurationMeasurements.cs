using Service.Interface.Observability;

namespace GerenciamentoMecanicaSistema.Observability;

public static class OrderDurationMeasurements
{
    public static void Record(IReadOnlyList<OrderHistoryEntry> history, string completedStatus, OrderBusinessMetrics metrics)
    {
        Validate(history);
        var index = history.ToList().FindIndex(entry => entry.NewStatus == completedStatus);
        if (index < 1)
            throw new InvalidOperationException("Intervalo concluído da ordem não encontrado.");
        var end = history[index];
        var start = history[index - 1];
        var seconds = (end.OccurredAt - start.OccurredAt).TotalSeconds;
        switch (completedStatus)
        {
            case "WaitingForApproval":
                metrics.RecordStage("diagnosis", seconds);
                break;
            case "Finished" when end.Reason == "ServiceCompleted":
                metrics.RecordStage("execution", seconds);
                metrics.RecordService((end.OccurredAt - history[0].OccurredAt).TotalSeconds);
                break;
            case "Delivered":
                metrics.RecordStage("delivery_wait", seconds,
                    start.Reason == "ServiceCompleted" ? "service_completed" : "budget_rejected");
                break;
            default:
                throw new InvalidOperationException("Intervalo concluído da ordem inválido.");
        }
    }

    private static void Validate(IReadOnlyList<OrderHistoryEntry> history)
    {
        if (history.Count == 0)
            throw new InvalidOperationException("Histórico da ordem ausente.");
        for (var index = 0; index < history.Count; index++)
        {
            var entry = history[index];
            var previous = index == 0 ? null : history[index - 1];
            var validTransition = (entry.PreviousStatus, entry.NewStatus) switch
            {
                (null, "Received") => index == 0,
                ("Received", "InDiagnosis") or ("InDiagnosis", "WaitingForApproval")
                    or ("WaitingForApproval", "WaitingForExecution") or ("WaitingForExecution", "InExecution")
                    or ("InExecution", "Finished") or ("WaitingForApproval", "Finished")
                    or ("Finished", "Delivered") => index > 0,
                _ => false
            };
            var expectedReason = (entry.NewStatus, entry.PreviousStatus) switch
            {
                ("Finished", "InExecution") => "ServiceCompleted",
                ("Finished", _) => "BudgetRejected",
                _ => null
            };
            if (entry.Sequence != index + 1 || !validTransition || entry.PreviousStatus != previous?.NewStatus
                || entry.Reason != expectedReason || entry.OccurredAt.Kind != DateTimeKind.Utc
                || entry.OccurredAt == DateTime.MinValue || entry.OccurredAt == DateTime.MaxValue
                || (previous is not null && entry.OccurredAt < previous.OccurredAt))
                throw new InvalidOperationException("Histórico da ordem incompatível com a medição.");
        }
    }
}
