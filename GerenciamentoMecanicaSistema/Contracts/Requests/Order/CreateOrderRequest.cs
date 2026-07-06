using Service.Interface.Commands.Order;
using GerenciamentoMecanicaSistema.Contracts.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Order
{
    public class CreateOrderRequest(string customerDocument, string vehicleLicensePlate)
    {
        [Description("Número do documento do cliente que requisitou o serviço")]
        [Required, RegularDocumentExpression]
        public string CustomerDocument { get; set; } = customerDocument;

        [Description("Placa do veículo a receber a manutenção")]
        [Required, RegularLicensePlateExpression]
        public string VehicleLicensePlate { get; set; } = vehicleLicensePlate;

        public CreateOrderCommand ToCommand() => new(CustomerDocument, VehicleLicensePlate);
    }
}
