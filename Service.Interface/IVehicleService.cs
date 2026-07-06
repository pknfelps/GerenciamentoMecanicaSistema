using Service.Interface.Commands.Vehicle;
using Service.Interface.Results.Vehicle;

namespace Service.Interface
{
    public interface IVehicleService
    {
        Task RegisterVehicle(CreateVehicleCommand vehicle);
        Task<IEnumerable<VehicleResult>> GetVehicles(Guid? id = null, string licensePlate = "");
        Task<VehicleResult?> GetVehicle(Guid? id = null, string licensePlate = "");
        Task UpdateVehicle(Guid id, CreateVehicleCommand vehicle);
        Task DeleteVehicle(Guid id);
    }
}
