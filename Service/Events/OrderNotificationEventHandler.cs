using Domain.Interface.Order;
using Microsoft.Extensions.Logging;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Events;
using Service.Interface.Events.Order;

namespace Service.Events
{
    public class OrderNotificationEventHandler(
        IOrderDependenciesGateway dependenciesGateway,
        IEmailService emailService,
        ILogger<OrderNotificationEventHandler> logger) : IApplicationEventHandler
    {
        private IOrderDependenciesGateway DependenciesGateway { get; } = dependenciesGateway;
        private IEmailService EmailService { get; } = emailService;
        private ILogger<OrderNotificationEventHandler> Logger { get; } = logger;

        public bool CanHandle(IApplicationEvent applicationEvent) =>
            applicationEvent is BudgetAvailableEvent or OrderStatusChangedEvent;

        public async Task Handle(IApplicationEvent applicationEvent)
        {
            var order = GetOrder(applicationEvent);

            if (order == null)
                return;

            try
            {
                var customer = await DependenciesGateway.GetCustomerByDocument(order.CustomerDocument.Id)
                    ?? throw new ApplicationFailureException("Falha ao notificar o cliente. Cliente não encontrado");
                var vehicle = await DependenciesGateway.GetVehicleByLicensePlate(order.VehicleLicensePlate.License)
                    ?? throw new ApplicationFailureException("Falha ao notificar o cliente. Veículo não encontrado");

                if (applicationEvent is BudgetAvailableEvent)
                    await EmailService.NotifyBudget(customer, vehicle, order);
                else
                    await EmailService.NotifyOrderStatus(customer, vehicle, order);
            }
            catch (Exception exception)
            {
                Logger.LogError(
                    exception,
                    "Falha ao enviar notificação da ordem. EventType: {EventType}. OrderId: {OrderId}. CustomerDocument: {CustomerDocument}. VehicleLicensePlate: {VehicleLicensePlate}",
                    applicationEvent.GetType().Name,
                    order.Id,
                    order.CustomerDocument.Id,
                    order.VehicleLicensePlate.License);
            }
        }

        private static IOrder? GetOrder(IApplicationEvent applicationEvent) =>
            applicationEvent switch
            {
                BudgetAvailableEvent budgetAvailable => budgetAvailable.Order,
                OrderStatusChangedEvent statusChanged => statusChanged.Order,
                _ => null
            };
    }
}
