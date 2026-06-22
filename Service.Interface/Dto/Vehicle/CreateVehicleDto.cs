using Domain.Interface.Vehicle;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Vehicle
{
    public class CreateVehicleDto(string customerDocument, string brand, string model, int year, string licensePlate)
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

        public virtual IVehicle ToDomain() => new Domain.Vehicle.Vehicle(CustomerDocument, Brand,Model, Year, LicensePlate);

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var vehicle = (CreateVehicleDto)obj;

            return Brand == vehicle.Brand && Model == vehicle.Model && Year == vehicle.Year && LicensePlate == vehicle.LicensePlate;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Brand, Model, Year, LicensePlate);
        }
    }
}
