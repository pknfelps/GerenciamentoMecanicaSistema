using Domain.Interface.Custumer;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.Interface.Vehicle;

namespace Service.Interface
{
    public interface IOrderDependenciesGateway
    {
        Task<ICustomer?> GetCustomerByDocument(string document);
        Task<IVehicle?> GetVehicleByLicensePlate(string licensePlate);
        Task<IMechanicalService?> GetServiceById(Guid id);
        Task<IMaterial?> GetMaterialById(Guid id);
    }
}
