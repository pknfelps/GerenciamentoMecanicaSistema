using Service.Interface.Dto.Order;

namespace Service.Interface
{
    public interface IWorkOrderService
    {
        Task CreateServiceOrder(CreateOrderDto orderToCreate);
        Task<IEnumerable<WorkOrderDto?>> GetOrders();
        Task<DetailedWorkOrderDto?> GetOrder(Guid orderId);
        Task<IEnumerable<DetailedWorkOrderDto?>> GetCustomerOrders(string customerDocument);
        Task StartDiagnosis(Guid orderId);
        Task AddServiceToOrder(OrderUpdateItemDto serviceDto);
        Task RemoveServiceOfOrder(OrderUpdateItemDto serviceDto);
        Task DeleteServiceFromOrder(OrderUpdateItemDto itemDto);
        Task AddPartOrSupplieToOrder(OrderUpdateItemDto orderItem);
        Task RemovePartOrSupplieFromOrder(OrderUpdateItemDto orderItem);
        Task CompleteDiagnosis(Guid orderId);
        Task ApproveBudget(ApproveOrderDto approve);
        Task StartExecution(Guid orderId);
        Task CompleteExecution(Guid orderId);
        Task VehicleDelivered(Guid orderId);
        Task DeleteOrder(Guid orderId);
    }
}
