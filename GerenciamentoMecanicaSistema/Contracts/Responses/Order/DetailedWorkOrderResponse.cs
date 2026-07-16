using GerenciamentoMecanicaSistema.Contracts.Responses.Catalog;
using GerenciamentoMecanicaSistema.Contracts.Responses.Stock;
using Service.Interface.Results.Order;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Order
{
    public class DetailedWorkOrderResponse(Guid id, string customerDocument, string vehicleLicensePlate, decimal budget, string status, DateTime dateCreated, DateTime dateFinished, List<ServiceResponse> services, List<MaterialResponse> materials, TimeSpan duration) : WorkOrderResponse(id, customerDocument, vehicleLicensePlate, budget, status, dateCreated, dateFinished, duration)
    {
        public List<ServiceResponse> Services { get; set; } = services;
        public List<MaterialResponse> Materials { get; set; } = materials;

        public static new DetailedWorkOrderResponse Create(DetailedWorkOrderResult order)
        {
            var services = order.Services.Select(ServiceResponse.Create);
            var materials = order.Materials.Select(MaterialResponse.Create);

            return new(order.Id, order.CustomerDocument, order.VehicleLicensePlate, order.Budget, order.Status, order.DateCreated, order.DateFinished, [.. services], [.. materials], order.Duration);
        }
    }
}
