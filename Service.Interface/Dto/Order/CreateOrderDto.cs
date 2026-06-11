using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Order
{
    public class CreateOrderDto(string customerDocument, string vehicleLicensePlate)
    {
        [Required, RegularDocumentExpression]
        public string CustomerDocument { get; private set; } = customerDocument;
        [Required, RegularExpression(@"^[a-zA-Z0-9]+$")]
        public string VehicleLicensePlate { get; private set; } = vehicleLicensePlate;
    }
}
