using Domain.Interface.Order;
using Service.Interface.Dto.Service;
using Service.Interface.Dto.Stock;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Order
{
    public class DetailedWorkOrderDto(Guid id, string costumerDocument, string vehicleLicensePlate, double budget, string status, DateTime dateCreated, DateTime dateFinished, List<ServiceDto> services, List<PartDto> parts) : WorkOrderDto(id, costumerDocument, vehicleLicensePlate, budget, status, dateCreated, dateFinished)
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
    }
}
