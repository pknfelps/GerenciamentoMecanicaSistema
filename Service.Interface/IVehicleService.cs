using Service.Interface.Dto.Vehicle;

namespace Service.Interface
{
    public interface IVehicleService
    {
        Task RegisterVehicle(CreateVehicleDto vehicleDto);
        Task<IEnumerable<VehicleDto>> GetVehicles(Guid? id = null, string licensePlate = "");
        Task<VehicleDto?> GetVehicle(Guid? id = null, string licensePlate = "");
        Task UpdateVehicle(Guid id, CreateVehicleDto vehicleDto);
        Task DeleteVehicle(Guid id);
    }
}
