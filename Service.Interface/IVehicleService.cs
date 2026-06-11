using Domain.Interface.Vehicle;
using Service.Interface.Dto.Vehicle;

namespace Service.Interface
{
    public interface IVehicleService
    {
        Task RegisterVehicle(CreateVehicleDto vehicleDto);
        Task<IVehicle> RegisterVehicleIfNotExists(VehicleDto vehicleDto);
        Task<IEnumerable<VehicleDto>> GetVehicles();
        Task<VehicleDto?> GetVehicle(string licensePlate);
        Task UpdateVehicle(VehicleDto vehicleDto);
        Task DeleteVehicle(string licensePlate);
    }
}
