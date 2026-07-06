using Domain.Interface.Order;

namespace Service.Interface.Results.Order
{
    public record WorkOrderResult(Guid Id, string CustomerDocument, string VehicleLicensePlate, double Budget, string Status, DateTime DateCreated, DateTime DateFinished, TimeSpan Duration)
    {
        public static WorkOrderResult Create(IOrder order) => new(order.Id, order.CustomerDocument.Id, order.VehicleLicensePlate.License, order.Budget, order.Status.ToString(), order.DateCreated, order.DateFinished, order.Duration);

        public static WorkOrderResult Create(DetailedWorkOrderResult order) => new(order.Id, order.CustomerDocument, order.VehicleLicensePlate, order.Budget, order.Status, order.DateCreated, order.DateFinished, order.Duration);
    }
}
