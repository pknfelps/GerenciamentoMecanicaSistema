using Service.Interface.Commands.Vehicle;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Vehicle
{
    public class CreateVehicleRequest(string customerDocument, string brand, string model, int year, string licensePlate)
    {
        [Description("Número do documento do dono do veículo")]
        [Required, RegularDocumentExpression]
        public string CustomerDocument { get; set; } = customerDocument;

        [Description("Marca do veículo")]
        [Required, RegularExpression(@"^[a-zA-Z]+$")]
        public string Brand { get; set; } = brand;

        [Description("Modelo do veículo")]
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ0-9\s]+$")]
        public string Model { get; set; } = model;

        [Description("Ano de fabricação do veículo")]
        [Required, RegularExpression(@"^\d{4}$")]
        public int Year { get; set; } = year;

        [Description("Placa do veículo")]
        [Required, RegularLicensePlateExpression]
        public string LicensePlate { get; set; } = licensePlate;

        public CreateVehicleCommand ToCommand() => new(CustomerDocument, Brand, Model, Year, LicensePlate);
    }
}
