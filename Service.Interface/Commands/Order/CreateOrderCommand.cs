namespace Service.Interface.Commands.Order
{
    public record CreateOrderCommand(
        Guid CustomerId,
        Guid VehicleId,
        IReadOnlyCollection<UpdateOrderItemCommand<int>> Services,
        IReadOnlyCollection<UpdateOrderItemCommand<int>> Materials);
}
