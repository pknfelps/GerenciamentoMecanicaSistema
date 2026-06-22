using Domain.Interface.Order;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Order
{
    public class WorkOrderDto(Guid id, string customerDocument, string vehicleLicensePlate, double budget, string status, DateTime dateCreated, DateTime dateFinished, TimeSpan duration)
    {
        [Description("Id único da ordem")]
        [Required, GuidValidation]
        public Guid Id { get; set; } = id;
        [Description("Documento do cliente")]
        [Required, RegularDocumentExpression]
        public string CustomerDocument { get; set; } = customerDocument;
        [Description("Placa do veículo da ordem")]
        [Required, RegularLicensePlateExpression]
        public string VehicleLicensePlate { get; set; } = vehicleLicensePlate;
        [Description("Orçamento da ordem")]
        [Required, Range(0, double.MaxValue)]
        public double Budget { get; set; } = budget;
        [Required, RegularNonEmptyStringExpression]
        [Description("Status da ordem")]
        public string Status { get; set; } = status;
        [Description("Data de criação da ordem")]
        [Required, DateTimeValidation]
        public DateTime DateCreated { get; set; } = dateCreated;
        [Description("Data de conclusão da ordem")]
        [Required]
        public DateTime DateFinished { get; set; } = dateFinished;
        [Description("Duração da ordem")]
        [Required]
        public TimeSpan Duration { get; set; } = duration;

        public static WorkOrderDto Create(IOrder order) => new(order.Id, order.CustomerDocument.Id, order.VehicleLicensePlate.License, order.Budget, order.Status.ToString(), order.DateCreated, order.DateFinished, order.Duration);

        public static WorkOrderDto Create(DetailedWorkOrderDto order) => new(order.Id, order.CustomerDocument, order.VehicleLicensePlate, order.Budget, order.Status.ToString(), order.DateCreated, order.DateFinished, order.Duration);

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;

            var order = (WorkOrderDto)obj;

            return Id == order.Id && CustomerDocument == order.CustomerDocument && VehicleLicensePlate == order.VehicleLicensePlate && Budget == order.Budget && Status == order.Status && DateCreated == order.DateCreated && DateFinished == order.DateFinished && Duration == order.Duration;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, CustomerDocument, VehicleLicensePlate, Budget, Status, DateCreated, DateFinished, Duration);
        }
    }
}
