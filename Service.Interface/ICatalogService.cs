using Service.Interface.Commands.Catalog;
using Service.Interface.Results.Catalog;

namespace Service.Interface
{
    public interface ICatalogService
    {
        Task RegisterService(CreateServiceCommand service);
        Task<IEnumerable<ServiceResult>> GetServices(Guid? id = null, string description = "");
        Task<ServiceResult?> GetService(Guid? id = null, string description = "");
        Task UpdateService(Guid serviceId, CreateServiceCommand service);
        Task DeleteService(Guid serviceId);
    }
}
