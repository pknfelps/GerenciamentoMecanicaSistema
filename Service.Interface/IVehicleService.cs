using Service.Interface.Dto;

namespace Service.Interface
{
    public interface IVehicleService
    {
        Task RegisterVehicle(VehicleDto vehicleDto);
        Task<IEnumerable<VehicleDto>> GetVehicles();
        Task<VehicleDto?> GetVehicle(string licensePlate);
        Task UpdateVehicle(VehicleDto vehicleDto);
        Task DeleteVehicle(string licensePlate);
    }
}
