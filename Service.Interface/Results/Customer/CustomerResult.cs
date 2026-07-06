namespace Service.Interface.Results.Customer
{
    public record CustomerResult(Guid Id, string Name, string Document, string Phone, string Email);
}
