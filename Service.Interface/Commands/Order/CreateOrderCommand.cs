namespace Service.Interface.Commands.Order
{
    public record CreateOrderCommand(string CustomerDocument, string VehicleLicensePlate);
}
