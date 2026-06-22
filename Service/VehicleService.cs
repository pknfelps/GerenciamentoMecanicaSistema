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
            if (await Repository.GetVehicle(license_plate: LicensePlateWrapper.CreateLicensePlate(vehicleDto.LicensePlate).License) != null)
                throw new InvalidOperationException("Veiculo já registrado no sistema");

            var registry = await Repository.RegisterVehicle(vehicleDto.ToDomain());

            if (registry == 0)
                throw new InvalidOperationException("Falha ao registrar veículo");
        }

        public async Task<IEnumerable<VehicleDto>> GetVehicles(Guid? id = null, string licensePlate = "")
        {
            if (!string.IsNullOrEmpty(licensePlate))
                licensePlate = LicensePlateWrapper.CreateLicensePlate(licensePlate).License;

            var vehicles = await Repository.GetVehicles(id, licensePlate);

            return vehicles.Select(VehicleDto.Create);
        }

        public async Task<VehicleDto?> GetVehicle(Guid? id = null, string licensePlate = "")
        {
            if (!string.IsNullOrEmpty(licensePlate))
                licensePlate = LicensePlateWrapper.CreateLicensePlate(licensePlate).License;

            var vehicle = await Repository.GetVehicle(id, licensePlate);

            if (vehicle == null)
                return null;

            return VehicleDto.Create(vehicle);
        }

        public async Task UpdateVehicle(Guid id, CreateVehicleDto vehicleDto)
        {
            _ = await Repository.GetVehicle(id: id) ?? throw new InvalidOperationException("Veiculo não encontrado");

            var registry = await Repository.UpdateVehicle(vehicleDto.ToDomain());

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar veículo");
        }

        public async Task DeleteVehicle(Guid id)
        {
            _ = await Repository.GetVehicle(id: id) ?? throw new InvalidOperationException("Veiculo não encontrado");

            var registry = await Repository.DeleteVehicle(id);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao deletar veículo");
        }
    }
}