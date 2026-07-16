using Service.Interface.Commands.Order;
using Service.Interface.Results.Order;

namespace Service.Interface
{
    public interface IOrdersService
    {
        Task CreateServiceOrder(CreateOrderCommand orderToCreate);
        Task<IEnumerable<DetailedWorkOrderResult>> GetOrders(Guid? id = null, string customerDocument = "", string vehicleLicensePlate = "");
        Task<DetailedWorkOrderResult?> GetOrder(Guid? id = null, string customerDocument = "", string vehicleLicensePlate = "");
        Task StartDiagnosis(Guid orderId);
        Task AddServiceToOrder(Guid orderId, UpdateOrderItemCommand<int> service);
        Task RemoveServiceOfOrder(Guid orderId, UpdateOrderItemCommand<int> service);
        Task AddMaterialToOrder(Guid orderId, UpdateOrderItemCommand<int> orderItem);
        Task RemoveMaterialFromOrder(Guid orderId, UpdateOrderItemCommand<int> orderItem);
        Task CompleteDiagnosis(Guid orderId);
        Task ApproveBudget(Guid orderId, ApproveOrderCommand approve);
        Task StartExecution(Guid orderId);
        Task CompleteExecution(Guid orderId);
        Task DeliverVehicle(Guid orderId);
        Task DeleteOrder(Guid orderId);
    }
}
