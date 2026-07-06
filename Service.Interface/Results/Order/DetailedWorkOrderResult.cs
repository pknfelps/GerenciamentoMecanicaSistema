using Domain.Interface.Order;
using Service.Interface.Results.Catalog;
using Service.Interface.Results.Stock;

namespace Service.Interface.Results.Order
{
    public record DetailedWorkOrderResult(Guid Id, string CustomerDocument, string VehicleLicensePlate, double Budget, string Status, DateTime DateCreated, DateTime DateFinished, List<ServiceResult> Services, List<MaterialResult> Materials, TimeSpan Duration)
    {
        public static DetailedWorkOrderResult Create(IOrder order)
        {
            var services = order.Services.Select(service => new ServiceResult(service.Id, service.Description, service.Hours, service.PricePerHour, service.Amount));
            var materials = order.Materials.Select(material => new MaterialResult(material.Id, material.Name, material.Brand, material.Price, material.Amount, material.ReservedAmount));

            return new(order.Id, order.CustomerDocument.Id, order.VehicleLicensePlate.License, order.Budget, order.Status.ToString(), order.DateCreated, order.DateFinished, [.. services], [.. materials], order.Duration);
        }
    }
}
