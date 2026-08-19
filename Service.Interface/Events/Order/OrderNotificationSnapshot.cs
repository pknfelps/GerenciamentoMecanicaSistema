using Domain.Interface.Order;

namespace Service.Interface.Events.Order
{
    public sealed record OrderNotificationSnapshot(
        Guid OrderId,
        string CustomerDocument,
        string VehicleLicensePlate,
        decimal Budget,
        WorkOrderStatus Status);
}
