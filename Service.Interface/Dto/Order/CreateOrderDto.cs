using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Order
{
    public class CreateOrderDto(string customerDocument, string vehicleLicensePlate)
    {
        [Description("Número do documento do cliente que requisitou o serviço")]
        [Required, RegularDocumentExpression]
        public string CustomerDocument { get; set; } = customerDocument;
        [Description("Placa do veículo a receber a manutenção")]
        [Required, RegularLicensePlateExpression]
        public string VehicleLicensePlate { get; set; } = vehicleLicensePlate;

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            var order = (CreateOrderDto)obj;

            return CustomerDocument == order.CustomerDocument && VehicleLicensePlate == order.VehicleLicensePlate;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CustomerDocument, VehicleLicensePlate);
        }
    }
}
