namespace Service.Interface.Persistence;

public sealed record CustomerAccessSyncState(long Revision, string Operation, bool Pending);

public interface ICustomerAccessSyncRepository
{
    Task LockCustomersAsync();
    Task<CustomerAccessSyncState?> FindAsync(Guid customerId);
    Task<long> MarkPendingAsync(Guid customerId, string operation);
    Task<bool> ConfirmAsync(Guid customerId, long revision);
}
