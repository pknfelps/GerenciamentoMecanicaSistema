using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto.Service;

namespace Service
{
    public class MechanicalServiceService(IMechanicalServiceRepository repository) : IMechanicalServiceService
    {
        private IMechanicalServiceRepository Repository { get; set; } = repository;

        public async Task RegisterService(CreateServiceDto serviceDto)
        {
            var service = await Repository.GetService(serviceDto.Description);

            if (service != null)
                throw new InvalidOperationException("Serivço já cadastrado");

            var registry = await Repository.RegisterService(serviceDto.ToDomain());

            if (registry == 0)
                throw new InvalidOperationException("Falha ao registrar o serviço");
        }

        public async Task<IEnumerable<ServiceDto?>> GetServices()
        {
            var services = await Repository.GetServices();

            return services.Select(ServiceDto.Create);
        }

        public async Task<ServiceDto?> GetService(Guid serviceId)
        {
            var service = await Repository.GetService(serviceId);

            if (service == null)
                return null;

            return ServiceDto.Create(service);
        }

        public async Task<ServiceDto?> GetService(string description)
        {
            var service = await Repository.GetService(description);

            if (service == null)
                return null;

            return ServiceDto.Create(service);
        }

        public async Task UpdateService(ServiceDto serviceDto)
        {
            _ = await Repository.GetService(serviceDto.Id) ?? throw new InvalidOperationException("Serviço não encontrado");

            var registry = await Repository.UpdateService(serviceDto.ToDomain());

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar o serviço");
        }

        public async Task DeleteService(Guid serviceId)
        {
            _ = await Repository.GetService(serviceId) ?? throw new InvalidOperationException("Serviço não encontrado");

            var registry = await Repository.DeleteService(serviceId);

            if (registry == 0)
                throw new InvalidOperationException("Falha ao atualizar o serviço");
        }
    }
}
