namespace Service.Interface.Commands.Order
{
    public record ApproveOrderCommand(string CustomerDocument, bool Approved);
}
