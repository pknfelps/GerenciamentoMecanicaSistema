using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto.Service;

namespace Service
{
    public class CatalogService(ICatalogRepository repository) : ICatalogService
    {
        private ICatalogRepository Repository { get; set; } = repository;

        public async Task RegisterService(CreateServiceDto serviceDto)
        {
            if (await Repository.GetService(description: serviceDto.Description) != null)
                throw new InvalidOperationException("Serivço já cadastrado");

            var registry = await Repository.RegisterService(serviceDto.ToDomain());

            if (registry == 0)
                throw new InvalidOperationException("Falha ao registrar o serviço");
        }

        public async Task<IEnumerable<ServiceDto>> GetServices(Guid? id = null, string description = "")
        {
            var services = await Repository.GetServices(id, description);

            return services.Select(ServiceDto.Create);
        }

        public async Task<ServiceDto?> GetService(Guid? id = null, string description = "")
        {
            if (id == null && string.IsNullOrEmpty(description))
                throw new InvalidOperationException("Falha ao procurar serviço. Nenhum argumento fornecido");

            var services = await Repository.GetService(id, description);

            if (services == null)
                return null;

            return ServiceDto.Create(services);
        }

        public async Task UpdateService(Guid serviceId, CreateServiceDto serviceDto)
        {
            var service = await Repository.GetService(serviceId) ?? throw new InvalidOperationException("Serviço não encontrado");

            service.UpdateDescriptrion(serviceDto.Description);
            service.UpdateHours(serviceDto.Hours);
            service.UpdatePricePerHour(serviceDto.PricePerHour);

            var registry = await Repository.UpdateService(service);

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
