using Domain.Interface.Vehicle;
using Domain.Vehicle;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto;

namespace Service
{
    public class VehicleService(IVehicleRepository repository) : IVehicleService
    {
        private IVehicleRepository Repository { get; set; } = repository;

        public async Task RegisterVehicle(VehicleDto vehicleDto)
        {
            var vehicle = ToDomain(vehicleDto);

            if (await CheckIfVehicleExists(vehicle.LicensePlate.License))
                throw new InvalidOperationException("Veiculo já registrado no sistema");

            var registry = await Repository.RegisterVehicle(vehicle);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao registrar veículo");
        }

        public async Task<IEnumerable<VehicleDto>> GetVehicles()
        {
            var vehicles = await Repository.GetVehicles();

            return vehicles.Select(ToDto);
        }

        public async Task<VehicleDto?> GetVehicle(string licensePlate)
        {
            var vehicle = await Repository.GetVehicle(licensePlate);

            if (vehicle == null)
                return null;

            return ToDto(vehicle);
        }

        public async Task UpdateVehicle(VehicleDto vehicleDto)
        {
            var vehicle = ToDomain(vehicleDto);

            if (!await CheckIfVehicleExists(vehicle.LicensePlate.License))
                throw new InvalidOperationException("Veiculo não encontrado");

            var registry = await Repository.UpdateVehicle(vehicle);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar veículo");
        }

        public async Task DeleteVehicle(string licensePlate)
        {
            LicensePlate license = LicensePlateWrapper.CreateLicensePlate(licensePlate);

            if (!await CheckIfVehicleExists(license.License))
                throw new InvalidOperationException("Veiculo não encontrado");

            var registry = await Repository.DeleteVehicle(licensePlate);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao deletar veículo");
        }

        private async Task<bool> CheckIfVehicleExists(string licensePlate) 
        {
            var vehicle = await Repository.GetVehicle(licensePlate);

            return vehicle != null;
        }

        private static VehicleDto ToDto(IVehicle vehicle) => new(vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.LicensePlate.License);

        private static IVehicle ToDomain(VehicleDto vehicle) => new Vehicle(vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.LicensePlate);
    }
}