using Domain.Interface.Vehicle;

namespace Service.Interface.Results.Vehicle
{
    public record VehicleResult(Guid Id, string CustomerDocument, string Brand, string Model, int Year, string LicensePlate)
    {
        public static VehicleResult Create(IVehicle vehicle) => new(vehicle.Id, vehicle.CustomerDocument.Id, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.LicensePlate.License);
    }
}
