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
        Task<int> UpdateOrder(IOrder order);
        Task<int> AddServiceToOrder(Guid orderId, IMechanicalService service);
        Task<int> UpdateServiceOfOrder(Guid orderId, IMechanicalService service);
        Task<int> RemoveServiceFromOrder(Guid orderId, Guid serviceId);
        Task<int> AddMaterialToOrder(Guid orderId, IMaterial material);
        Task<int> UpdateMaterialFromOrder(Guid orderId, IMaterial material);
        Task<int> RemoveMaterialFromOrder(Guid orderId, Guid materialId);
        Task<int> DeleteOrder(Guid orderId);
    }
}
