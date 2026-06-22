using Service.Interface.Dto.Service;

namespace Service.Interface
{
    public interface ICatalogService
    {
        Task RegisterService(CreateServiceDto serviceDto);
        Task<IEnumerable<ServiceDto>> GetServices(Guid? id = null, string description = "");
        Task<ServiceDto?> GetService(Guid? id = null, string description = "");
        Task UpdateService(Guid serviceId, CreateServiceDto serviceDto);
        Task DeleteService(Guid serviceId);
    }
}
