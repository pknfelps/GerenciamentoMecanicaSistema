using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto
{
    public class VehicleDto(string brand, string model, int year, string licensePlate)
    {
        [Required, RegularExpression(@"^[a-zA-Z]+$")]
        public string Brand { get; private set; } = brand;
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ0-9\s]+$")]
        public string Model { get; private set; } = model;
        [Required, RegularExpression(@"^\d{4}$")]
        public int Year { get; private set; } = year;
        [Required, RegularExpression(@"^[a-zA-Z0-9]+$")]
        public string LicensePlate { get; private set; } = licensePlate;

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var vehicle = (VehicleDto)obj;

            return Brand == vehicle.Brand && Model == vehicle.Model && Year == vehicle.Year && LicensePlate == vehicle.LicensePlate;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Brand, Model, Year, LicensePlate);
        }
    }
}
