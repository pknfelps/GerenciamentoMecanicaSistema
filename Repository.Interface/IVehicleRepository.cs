using Domain.Interface.Vehicle;

namespace Repository.Interface
{
    public interface IVehicleRepository
    {
        Task<int> RegisterVehicle(IVehicle vehicle);
        Task<IEnumerable<IVehicle>> GetVehicles();
        Task<IVehicle?> GetVehicle(string licensePlate);
        Task<int> UpdateVehicle(IVehicle vehicle);
        Task<int> DeleteVehicle(Guid vehicleId);
    }
}
