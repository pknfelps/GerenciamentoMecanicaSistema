using Domain.Interface.Order;

namespace Service.Interface.Events.Order
{
    public sealed record BudgetAvailableEvent(IOrder Order) : IApplicationEvent;
}
