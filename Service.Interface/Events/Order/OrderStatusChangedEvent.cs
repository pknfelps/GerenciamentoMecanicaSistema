namespace Service.Interface.Events.Order
{
    public sealed record OrderStatusChangedEvent(OrderNotificationSnapshot Order) : IApplicationEvent;
}
