using Service.Interface;
using Service.Interface.Events;
using Service.Interface.Events.Order;

namespace Service.Events
{
    public class BudgetAvailableEventHandler(IOrderDependenciesGateway dependenciesGateway, IEmailService emailService) : IApplicationEventHandler
    {
        private IOrderDependenciesGateway DependenciesGateway { get; } = dependenciesGateway;
        private IEmailService EmailService { get; } = emailService;

        public bool CanHandle(IApplicationEvent applicationEvent) => applicationEvent is BudgetAvailableEvent;

        public async Task Handle(IApplicationEvent applicationEvent)
        {
            if (applicationEvent is not BudgetAvailableEvent budgetAvailable)
                return;

            try
            {
                var order = budgetAvailable.Order;
                var customer = await DependenciesGateway.GetCustomerByDocument(order.CustomerDocument.Id) ?? throw new InvalidOperationException("Falha ao notificar o cliente. Cliente não encontrado");
                var vehicle = await DependenciesGateway.GetVehicleByLicensePlate(order.VehicleLicensePlate.License) ?? throw new InvalidOperationException("Falha ao notificar o cliente. Veículo não encontrado");

                await EmailService.NotifyBudget(customer, vehicle, order);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Falha ao enviar email para o cliente. {e}");
            }
        }
    }
}
