using Domain.Vehicle;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Vehicle;
using Service.Interface.Results.Vehicle;

namespace Service
{
    public class VehicleService(IVehicleRepository repository) : IVehicleService
    {
        private IVehicleRepository Repository { get; set; } = repository;

        public async Task RegisterVehicle(CreateVehicleCommand vehicle)
        {
            if (await Repository.GetVehicle(license_plate: LicensePlateWrapper.CreateLicensePlate(vehicle.LicensePlate).License) != null)
                throw new ConflictException("Veiculo jÃ¡ registrado no sistema");

            var registry = await Repository.RegisterVehicle(CreateDomain(vehicle));

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao registrar veÃ­culo");
        }

        public async Task<IEnumerable<VehicleResult>> GetVehicles(Guid? id = null, string licensePlate = "")
        {
            if (!string.IsNullOrEmpty(licensePlate))
                licensePlate = LicensePlateWrapper.CreateLicensePlate(licensePlate).License;

            var vehicles = await Repository.GetVehicles(id, licensePlate);

            return vehicles.Select(VehicleResult.Create);
        }

        public async Task<VehicleResult?> GetVehicle(Guid? id = null, string licensePlate = "")
        {
            if (!string.IsNullOrEmpty(licensePlate))
                licensePlate = LicensePlateWrapper.CreateLicensePlate(licensePlate).License;

            var vehicle = await Repository.GetVehicle(id, licensePlate);

            if (vehicle == null)
                return null;

            return VehicleResult.Create(vehicle);
        }

        public async Task UpdateVehicle(Guid id, CreateVehicleCommand vehicle)
        {
            _ = await Repository.GetVehicle(id: id) ?? throw new NotFoundException("Veiculo nÃ£o encontrado");

            var registry = await Repository.UpdateVehicle(CreateDomain(vehicle));

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao atualizar veÃ­culo");
        }

        public async Task DeleteVehicle(Guid id)
        {
            _ = await Repository.GetVehicle(id: id) ?? throw new NotFoundException("Veiculo nÃ£o encontrado");

            var registry = await Repository.DeleteVehicle(id);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao deletar veÃ­culo");
        }

        private static Domain.Vehicle.Vehicle CreateDomain(CreateVehicleCommand vehicle) => new(vehicle.CustomerDocument, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.LicensePlate);
    }
}
