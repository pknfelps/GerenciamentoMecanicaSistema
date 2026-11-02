namespace Service.Interface.Observability;

public interface IOrderMetricsHistoryReader
{
    Task<IReadOnlyList<OrderHistoryEntry>> ReadAsync(Guid orderId, int timeoutSeconds, CancellationToken cancellationToken);
}
