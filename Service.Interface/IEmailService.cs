using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Vehicle;

namespace Service.Interface
{
    public interface IEmailService
    {
        Task NotifyBudget(ICustomer customer, IVehicle vehicle, IOrder order);
    }
}
