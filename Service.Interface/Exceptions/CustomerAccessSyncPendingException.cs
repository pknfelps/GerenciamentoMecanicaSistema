namespace Service.Interface.Exceptions;

public sealed class CustomerAccessSyncPendingException(Guid customerId) : ApplicationBaseException(
    "Alteração cadastral registrada. A atualização do acesso do cliente está pendente. Tente novamente.")
{
    public const string Code = "CUSTOMER_ACCESS_SYNC_PENDING";
    public Guid CustomerId { get; } = customerId;
}
