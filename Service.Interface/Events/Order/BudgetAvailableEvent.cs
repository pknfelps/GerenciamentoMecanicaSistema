namespace Service.Interface.Events.Order
{
    public sealed record BudgetAvailableEvent(OrderNotificationSnapshot Order) : IApplicationEvent;
}
