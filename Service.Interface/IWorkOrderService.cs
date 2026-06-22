using Service.Interface.Dto;
using Service.Interface.Dto.Order;

namespace Service.Interface
{
    public interface IWorkOrderService
    {
        Task CreateServiceOrder(CreateOrderDto orderToCreate);
        Task<IEnumerable<DetailedWorkOrderDto>> GetOrders(Guid? id = null, string customerDocument = "", string vehicleLicensePlate = "");
        Task<DetailedWorkOrderDto?> GetOrder(Guid? id = null, string customerDocument = "", string vehicleLicensePlate = "");
        Task StartDiagnosis(Guid orderId);
        Task AddServiceToOrder(Guid orderId, UpdateItemDto<int> serviceDto);
        Task RemoveServiceOfOrder(Guid orderId, UpdateItemDto<int> serviceDto);
        Task AddPartToOrder(Guid orderId, UpdateItemDto<int> orderItem);
        Task RemovePartFromOrder(Guid orderId, UpdateItemDto<int> orderItem);
        Task CompleteDiagnosis(Guid orderId);
        Task ApproveBudget(Guid orderId, ApproveOrderDto approve);
        Task StartExecution(Guid orderId);
        Task CompleteExecution(Guid orderId);
        Task DeliverVehicle(Guid orderId);
        Task DeleteOrder(Guid orderId);
    }
}
