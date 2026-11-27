namespace Service.Interface.CustomerAccess;

public sealed class CustomerAccessData(string name, string document, string email, string status)
{
    public string Name { get; } = name;
    public string Document { get; } = document;
    public string Email { get; } = email;
    public string Status { get; } = status;
}

public sealed class CustomerAccessCommand(Guid customerId, long revision, CustomerAccessData? access)
{
    public const int ContractVersion = 1;
    public string Operation { get; } = "sync-customer-access";
    public int Version { get; } = ContractVersion;
    public Guid CustomerId { get; } = customerId;
    public long Revision { get; } = revision;
    public string Action => Access is null ? "revoke" : "upsert";
    public CustomerAccessData? Access { get; } = access;
}
