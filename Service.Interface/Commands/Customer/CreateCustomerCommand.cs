namespace Service.Interface.Commands.Customer
{
    public record CreateCustomerCommand(string Name, string Document, string Phone, string Email);
}
