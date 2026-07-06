namespace Service.Interface.Commands.Order
{
    public record UpdateOrderItemCommand<T>(Guid Id, T Value);
}
