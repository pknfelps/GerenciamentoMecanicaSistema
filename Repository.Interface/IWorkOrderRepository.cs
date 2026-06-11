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
        Task<IMechanicalService?> GetServiceFromOrder(Guid serviceId, Guid orderId);
        Task<int> UpdateOrderStatus(Guid orderId, ServiceOrderStatus status);
        Task<int> AddServiceToOrder(Guid orderId, IMechanicalService service);
        Task<int> UpdateServiceOfOrder(Guid orderId, IMechanicalService service);
        Task<int> DeleteServiceFromOrder(Guid orderId, Guid serviceId);
        Task<int> AddPartOrSupplieToOrder(Guid orderId, IPart part);
        Task<int> RemovePartOrSupplieFromOrder(Guid orderId, Guid partId);
        Task<int> UpdateOrderBudget(Guid id, double budget);
        Task<int> UpdatePartOrSupplieFromOrder(Guid orderId, IPart part);
        Task<int> DeleteOrder(Guid orderId);
    }
}
