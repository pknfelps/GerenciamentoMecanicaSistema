using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;

namespace Repository.Interface
{
    public interface IOrdersRepository
    {
        Task<int> CreateOrder(IOrder serviceOrder);
        Task<IEnumerable<IOrder>> GetOrders(Guid? id = null, string customer_document = "", string vehicle_license_plate = "");
        Task<IOrder?> GetOrder(Guid? id = null, string customer_document = "", string vehicle_license_plate = "");
        Task<int> UpdateOrderStatus(Guid orderId, WorkOrderStatus status);
        Task<int> AddServiceToOrder(Guid orderId, IMechanicalService service);
        Task<int> UpdateServiceOfOrder(Guid orderId, IMechanicalService service);
        Task<int> RemoveServiceFromOrder(Guid orderId, Guid serviceId);
        Task<int> AddMaterialToOrder(Guid orderId, IMaterial part);
        Task<int> UpdateMaterialFromOrder(Guid orderId, IMaterial part);
        Task<int> RemoveMaterialFromOrder(Guid orderId, Guid partId);
        Task<int> UpdateOrderBudget(Guid id, double budget);
        Task<int> UpdateOrderDuration(Guid id, TimeSpan duration);
        Task<int> DeleteOrder(Guid orderId);
    }
}
