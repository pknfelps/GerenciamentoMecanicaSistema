using Domain.Interface.Vehicle;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Vehicle
{
    public class VehicleDto(Guid id, string customerDocument, string brand, string model, int year, string licensePlate) : CreateVehicleDto(customerDocument, brand, model, year, licensePlate)
    {
        [Required, GuidValidation]
        public Guid Id { get; private set; } = id;

        public static VehicleDto Create(IVehicle vehicle) => new(vehicle.Id, vehicle.CustomerDocument.Id, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.LicensePlate.License);

        public override IVehicle ToDomain() => new Domain.Vehicle.Vehicle(Id, CustomerDocument, Brand, Model, Year, LicensePlate);
    }
}
