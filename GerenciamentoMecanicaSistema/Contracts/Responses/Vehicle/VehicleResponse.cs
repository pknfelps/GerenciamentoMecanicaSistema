using Service.Interface.Dto.CustomAttributes;
using Service.Interface.Results.Vehicle;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Vehicle
{
    public class VehicleResponse(Guid id, string customerDocument, string brand, string model, int year, string licensePlate)
    {
        [Description("Id único do veículo")]
        [Required, GuidValidation]
        public Guid Id { get; set; } = id;

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

        public static VehicleResponse Create(VehicleResult vehicle) => new(vehicle.Id, vehicle.CustomerDocument, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.LicensePlate);
    }
}
