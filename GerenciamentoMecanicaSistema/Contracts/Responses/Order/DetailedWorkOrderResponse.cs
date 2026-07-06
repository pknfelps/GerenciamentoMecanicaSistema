using GerenciamentoMecanicaSistema.Contracts.Responses.Catalog;
using GerenciamentoMecanicaSistema.Contracts.Responses.Stock;
using Service.Interface.Results.Order;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Order
{
    public class DetailedWorkOrderResponse(Guid id, string customerDocument, string vehicleLicensePlate, double budget, string status, DateTime dateCreated, DateTime dateFinished, List<ServiceResponse> services, List<MaterialResponse> materials, TimeSpan duration) : WorkOrderResponse(id, customerDocument, vehicleLicensePlate, budget, status, dateCreated, dateFinished, duration)
    {
        [Description("Lista de serviços da ordem")]
        [Required]
        public List<ServiceResponse> Services { get; set; } = services;

        [Description("Lista de itens da ordem")]
        [Required]
        public List<MaterialResponse> Materials { get; set; } = materials;

        public static new DetailedWorkOrderResponse Create(DetailedWorkOrderResult order)
        {
            var services = order.Services.Select(ServiceResponse.Create);
            var materials = order.Materials.Select(MaterialResponse.Create);

            return new(order.Id, order.CustomerDocument, order.VehicleLicensePlate, order.Budget, order.Status, order.DateCreated, order.DateFinished, [.. services], [.. materials], order.Duration);
        }
    }
}
