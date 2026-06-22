using Domain.Interface.Vehicle;

namespace Repository.Interface
{
    public interface IVehicleRepository
    {
        Task<int> RegisterVehicle(IVehicle vehicle);
        Task<IEnumerable<IVehicle>> GetVehicles(Guid? id = null, string license_plate = "");
        Task<IVehicle?> GetVehicle(Guid? id = null, string license_plate = "");
        Task<int> UpdateVehicle(IVehicle vehicle);
        Task<int> DeleteVehicle(Guid vehicleId);
    }
}
