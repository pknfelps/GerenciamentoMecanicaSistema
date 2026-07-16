using Service.Interface.Results.Order;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Order
{
    public class WorkOrderResponse(Guid id, string customerDocument, string vehicleLicensePlate, decimal budget, string status, DateTime dateCreated, DateTime dateFinished, TimeSpan duration)
    {
        public Guid Id { get; set; } = id;
        public string CustomerDocument { get; set; } = customerDocument;
        public string VehicleLicensePlate { get; set; } = vehicleLicensePlate;
        public decimal Budget { get; set; } = budget;
        public string Status { get; set; } = status;
        public DateTime DateCreated { get; set; } = dateCreated;
        public DateTime DateFinished { get; set; } = dateFinished;
        public TimeSpan Duration { get; set; } = duration;

        public static WorkOrderResponse Create(WorkOrderResult order) => new(order.Id, order.CustomerDocument, order.VehicleLicensePlate, order.Budget, order.Status, order.DateCreated, order.DateFinished, order.Duration);

        public static WorkOrderResponse Create(DetailedWorkOrderResult order) => Create(WorkOrderResult.Create(order));
    }
}
