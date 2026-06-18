using Domain.Interface.Order;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Order
{
    public class WorkOrderDto(Guid id, string customerDocument, string vehicleLicensePlate, double budget, string status, DateTime dateCreated, DateTime dateFinished, TimeSpan duration)
    {
        [GuidValidation]
        public Guid Id { get; private set; } = id;
        [Required, RegularDocumentExpression]
        public string CustomerDocument { get; private set; } = customerDocument;
        [Required, RegularLicensePlateExpression]
        public string VehicleLicensePlate { get; private set; } = vehicleLicensePlate;
        [Required, Range(0, double.MaxValue)]
        public double Budget { get; private set; } = budget;
        [Required]
        public string Status { get; private set; } = status;
        [Required, DateTimeValidation]
        public DateTime DateCreated { get; private set; } = dateCreated;
        [Required]
        public DateTime DateFinished { get; private set; } = dateFinished;
        [Required]
        public TimeSpan Duration { get; private set; } = duration;

        public static WorkOrderDto Create(IWorkOrder order) => new(order.Id, order.CustomerDocument.Id, order.VehicleLicensePlate.License, order.Budget, order.Status.ToString(), order.DateCreated, order.DateFinished, order.Duration);

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;

            var order = (WorkOrderDto)obj;

            return Id == order.Id && CustomerDocument == order.CustomerDocument && VehicleLicensePlate == order.VehicleLicensePlate && Budget == order.Budget && Status == order.Status && DateCreated == order.DateCreated && DateFinished == order.DateFinished && Duration == order.Duration;
        }
    }
}
