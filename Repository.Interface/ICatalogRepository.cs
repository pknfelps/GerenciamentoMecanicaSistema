using Domain.Interface.Service;

namespace Repository.Interface
{
    public interface ICatalogRepository
    {
        Task<int> RegisterService(IMechanicalService service);
        Task<IEnumerable<IMechanicalService>> GetServices(Guid? id = null, string description = "");
        Task<IMechanicalService?> GetService(Guid? id = null, string description = "");
        Task<int> UpdateService(IMechanicalService service);
        Task<int> DeleteService(Guid serviceId);
    }
}
