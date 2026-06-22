using Domain.Interface.Order;
using Service.Interface.Dto.Service;
using Service.Interface.Dto.Stock;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Order
{
    public class DetailedWorkOrderDto(Guid id, string customerDocument, string vehicleLicensePlate, double budget, string status, DateTime dateCreated, DateTime dateFinished, List<ServiceDto> services, List<PartDto> parts, TimeSpan duration) : WorkOrderDto(id, customerDocument, vehicleLicensePlate, budget, status, dateCreated, dateFinished, duration)
    {
        [Description("Lista de serviços da ordem")]
        [Required]
        public List<ServiceDto> Services { get; set; } = services;
        [Description("Lista de itens da ordem")]
        [Required]
        public List<PartDto> Parts { get; set; } = parts;

        public static new DetailedWorkOrderDto Create(IOrder order)
        {
            var services = order.Services.Select(ServiceDto.Create);
            var partsAndSupplies = order.Parts.Select(PartDto.Create);

            return new(order.Id, order.CustomerDocument.Id, order.VehicleLicensePlate.License, order.Budget, order.Status.ToString(), order.DateCreated, order.DateFinished, [.. services], [.. partsAndSupplies], order.Duration);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;

            var order = (DetailedWorkOrderDto)obj;

            if (Services.Count != order.Services.Count || Parts.Count != order.Parts.Count)
                return false;

            var differentServices = order.Services.Select(x => Services.Exists(y => !y.Equals(x)));

            if ((differentServices.ToList()).Count != 0)
                return false;

            var differentParts = order.Parts.Select(x => Parts.Exists(y => !y.Equals(x)));

            if ((differentParts.ToList()).Count != 0)
                return false;

            return Id == order.Id && CustomerDocument == order.CustomerDocument && VehicleLicensePlate == order.VehicleLicensePlate && Budget == order.Budget && Status == order.Status && DateCreated == order.DateCreated && DateFinished == order.DateFinished && Duration == order.Duration;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, CustomerDocument, VehicleLicensePlate, Budget, Status, DateCreated, DateFinished, Duration);
        }
    }
}
