using Domain.Interface.Order;

namespace Service.Interface.Events.Order
{
    public sealed record OrderStatusChangedEvent(IOrder Order) : IApplicationEvent;
}
