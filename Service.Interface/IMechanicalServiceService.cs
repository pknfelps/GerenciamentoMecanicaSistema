using Service.Interface.Dto.Service;

namespace Service.Interface
{
    public interface IMechanicalServiceService
    {
        Task RegisterService(CreateServiceDto serviceDto);
        Task<IEnumerable<ServiceDto?>> GetServices();
        Task<ServiceDto?> GetService(Guid serviceId);
        Task<ServiceDto?> GetService(string description);
        Task UpdateService(ServiceDto serviceDto);
        Task DeleteService(Guid serviceId);
    }
}
