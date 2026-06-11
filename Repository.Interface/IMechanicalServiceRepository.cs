using Domain.Interface.Service;

namespace Repository.Interface
{
    public interface IMechanicalServiceRepository
    {
        Task<int> RegisterService(IMechanicalService service);
        Task<IEnumerable<IMechanicalService?>> GetServices();
        Task<IMechanicalService?> GetService(Guid serviceId);
        Task<IMechanicalService?> GetService(string description);
        Task<int> UpdateService(IMechanicalService service);
        Task<int> DeleteService(Guid serviceId);
    }
}
