using Domain.Interface.Custumer;
using Domain.Interface.Vehicle;
using Service.Interface.Events.Order;

namespace Service.Interface
{
    public interface IEmailService
    {
        Task NotifyBudget(ICustomer customer, IVehicle vehicle, OrderNotificationSnapshot order);
        Task NotifyOrderStatus(ICustomer customer, IVehicle vehicle, OrderNotificationSnapshot order);
    }
}
