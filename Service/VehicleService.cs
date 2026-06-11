using Domain.Customer;
using Domain.Interface.Vehicle;
using Domain.Vehicle;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto.Vehicle;

namespace Service
{
    public class VehicleService(IVehicleRepository repository) : IVehicleService
    {
        private IVehicleRepository Repository { get; set; } = repository;

        public async Task RegisterVehicle(CreateVehicleDto vehicleDto)
        {
            var vehicle = vehicleDto.ToDomain();

            if (await CheckIfVehicleExists(vehicle.LicensePlate.License))
                throw new InvalidOperationException("Veiculo já registrado no sistema");

            var registry = await Repository.RegisterVehicle(vehicle);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao registrar veículo");
        }

        public async Task<IVehicle> RegisterVehicleIfNotExists(VehicleDto vehicleDto)
        {
            var licensePlate = LicensePlateWrapper.CreateLicensePlate(vehicleDto.LicensePlate);

            var vehicle = await Repository.GetVehicle(licensePlate.License);

            if (vehicle != null)
                return vehicle;

            var registry = await Repository.RegisterVehicle(vehicleDto.ToDomain());

            if (registry == 0)
                throw new InvalidOperationException("Falha ao registrar veículo");

            return await Repository.GetVehicle(licensePlate.License);
        }

        public async Task<IEnumerable<VehicleDto>> GetVehicles()
        {
            var vehicles = await Repository.GetVehicles();

            return vehicles.Select(VehicleDto.Create);
        }

        public async Task<VehicleDto?> GetVehicle(string licensePlate)
        {
            var vehicle = await Repository.GetVehicle(licensePlate);

            if (vehicle == null)
                return null;

            return VehicleDto.Create(vehicle);
        }

        public async Task UpdateVehicle(VehicleDto vehicleDto)
        {
            var vehicle = vehicleDto.ToDomain();

            if (!await CheckIfVehicleExists(vehicle.LicensePlate.License))
                throw new InvalidOperationException("Veiculo não encontrado");

            var registry = await Repository.UpdateVehicle(vehicle);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar veículo");
        }

        public async Task DeleteVehicle(string licensePlate)
        {
            LicensePlate license = LicensePlateWrapper.CreateLicensePlate(licensePlate);

            var vehicle = await Repository.GetVehicle(license.License) ?? throw new InvalidOperationException("Veiculo não encontrado");

            var registry = await Repository.DeleteVehicle(vehicle.Id);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao deletar veículo");
        }

        private async Task<bool> CheckIfVehicleExists(string licensePlate) 
        {
            var vehicle = await Repository.GetVehicle(licensePlate);

            return vehicle != null;
        }
    }
}