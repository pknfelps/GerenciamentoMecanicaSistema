using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;

namespace Repository.Interface
{
    public interface IWorkOrderRepository
    {
        Task<int> CreateOrder(IWorkOrder serviceOrder);
        Task<IEnumerable<IWorkOrder?>> GetOrders();
        Task<IWorkOrder?> GetOrder(Guid orderId);
        Task<IEnumerable<IWorkOrder?>> GetCustomerOrders(string customerDocument);
        Task<int> UpdateOrderStatus(Guid orderId, WorkOrderStatus status);
        Task<int> AddServiceToOrder(Guid orderId, IMechanicalService service);
        Task<int> UpdateServiceOfOrder(Guid orderId, IMechanicalService service);
        Task<int> DeleteServiceFromOrder(Guid orderId, Guid serviceId);
        Task<int> AddPartToOrder(Guid orderId, IPart part);
        Task<int> UpdatePartFromOrder(Guid orderId, IPart part);
        Task<int> RemovePartFromOrder(Guid orderId, Guid partId);
        Task<int> UpdateOrderBudget(Guid id, double budget);
        Task<int> UpdateOrderDuration(Guid id, TimeSpan duration);
        Task<int> DeleteOrder(Guid orderId);
    }
}
