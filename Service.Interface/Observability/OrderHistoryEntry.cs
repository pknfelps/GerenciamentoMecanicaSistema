namespace Service.Interface.Observability;

public sealed class OrderHistoryEntry
{
    public int Sequence { get; init; }
    public string? PreviousStatus { get; init; }
    public string NewStatus { get; init; } = "";
    public DateTime OccurredAt { get; init; }
    public string? Reason { get; init; }
}
