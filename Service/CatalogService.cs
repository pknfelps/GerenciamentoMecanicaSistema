using Domain.Interface.Service;
using Domain.MechanicalService;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Catalog;
using Service.Interface.Results.Catalog;

namespace Service
{
    public class CatalogService(ICatalogRepository repository) : ICatalogService
    {
        private ICatalogRepository Repository { get; set; } = repository;

        public async Task RegisterService(CreateServiceCommand service)
        {
            if (await Repository.GetService(description: service.Description) != null)
                throw new ConflictException("Serviço já cadastrado");

            IMechanicalService serviceToRegister = new MechanicalService(service.Description, service.Hours, service.PricePerHour);

            var registry = await Repository.RegisterService(serviceToRegister);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao registrar o serviço");
        }

        public async Task<IEnumerable<ServiceResult>> GetServices(Guid? id = null, string description = "")
        {
            var services = await Repository.GetServices(id, description);

            return services.Select(CreateResult);
        }

        public async Task<ServiceResult?> GetService(Guid? id = null, string description = "")
        {
            if (id == null && string.IsNullOrEmpty(description))
                throw new InvalidRequestException("Falha ao procurar serviço. Nenhum argumento fornecido");

            var service = await Repository.GetService(id, description);

            if (service == null)
                return null;

            return CreateResult(service);
        }

        public async Task UpdateService(Guid serviceId, CreateServiceCommand service)
        {
            var serviceToUpdate = await Repository.GetService(serviceId) ?? throw new NotFoundException("Serviço não encontrado");

            serviceToUpdate.UpdateDescriptrion(service.Description);
            serviceToUpdate.UpdateHours(service.Hours);
            serviceToUpdate.UpdatePricePerHour(service.PricePerHour);

            var registry = await Repository.UpdateService(serviceToUpdate);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao atualizar o serviço");
        }

        public async Task DeleteService(Guid serviceId)
        {
            _ = await Repository.GetService(serviceId) ?? throw new NotFoundException("Serviço não encontrado");

            var registry = await Repository.DeleteService(serviceId);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao atualizar o serviço");
        }

        private static ServiceResult CreateResult(IMechanicalService service)
        {
            return new(service.Id, service.Description, service.Hours, service.PricePerHour, service.Amount);
        }
    }
}
