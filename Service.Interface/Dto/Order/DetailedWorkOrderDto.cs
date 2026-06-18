using Domain.Interface.Order;
using Service.Interface.Dto.Service;
using Service.Interface.Dto.Stock;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Order
{
    public class DetailedWorkOrderDto(Guid id, string customerDocument, string vehicleLicensePlate, double budget, string status, DateTime dateCreated, DateTime dateFinished, List<ServiceDto> services, List<PartDto> parts) : WorkOrderDto(id, customerDocument, vehicleLicensePlate, budget, status, dateCreated, dateFinished)
    {
        [Required]
        public List<ServiceDto> Services { get; private set; } = services;
        [Required]
        public List<PartDto> Parts { get; private set; } = parts;

        public static DetailedWorkOrderDto Create(IWorkOrder order)
        {
            var services = order.Services.Select(ServiceDto.Create);
            var partsAndSupplies = order.Parts.Select(PartDto.Create);

            return new(order.Id, order.CustomerDocument.Id, order.VehicleLicensePlate.License, order.Budget, order.Status.ToString(), order.DateCreated, order.DateFinished, [.. services], [.. partsAndSupplies]);
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

            return Id == order.Id && CustomerDocument == order.CustomerDocument && VehicleLicensePlate == order.VehicleLicensePlate && Budget == order.Budget && Status == order.Status && DateCreated == order.DateCreated && DateFinished == order.DateFinished;
        }
    }
}
