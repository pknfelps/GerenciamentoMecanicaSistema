namespace Service.Interface.CustomerAccess;

public enum CustomerAccessSyncOutcome { Pending, Applied, DocumentConflict }

public interface ICustomerAccessSynchronizer
{
    Task<CustomerAccessSyncOutcome> SynchronizeAsync(CustomerAccessCommand command);
}
