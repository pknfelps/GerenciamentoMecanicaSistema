using Microsoft.Extensions.Logging;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Events;
using Service.Interface.Events.Order;
using Service.Interface.Observability;
using Service.Observability;

namespace Service.Events
{
    public class OrderNotificationEventHandler(
        IOrderDependenciesGateway dependenciesGateway,
        IEmailService emailService,
        ILogger<OrderNotificationEventHandler> logger,
        IOrderNotificationMetrics? metrics = null) : IApplicationEventHandler
    {
        private IOrderDependenciesGateway DependenciesGateway { get; } = dependenciesGateway;
        private IEmailService EmailService { get; } = emailService;
        private ILogger<OrderNotificationEventHandler> Logger { get; } = logger;
        private IOrderNotificationMetrics Metrics { get; } = metrics ?? new NullOrderNotificationMetrics();

        public bool CanHandle(IApplicationEvent applicationEvent) =>
            applicationEvent is BudgetAvailableEvent or OrderStatusChangedEvent;

        public async Task Handle(IApplicationEvent applicationEvent)
        {
            var order = GetOrder(applicationEvent);

            if (order == null)
                return;

            var succeeded = false;
            try
            {
                var customer = await DependenciesGateway.GetCustomerByDocument(order.CustomerDocument)
                    ?? throw new ApplicationFailureException("Falha ao notificar o cliente. Cliente não encontrado");
                var vehicle = await DependenciesGateway.GetVehicleByLicensePlate(order.VehicleLicensePlate)
                    ?? throw new ApplicationFailureException("Falha ao notificar o cliente. Veículo não encontrado");

                if (applicationEvent is BudgetAvailableEvent)
                    await EmailService.NotifyBudget(customer, vehicle, order);
                else
                    await EmailService.NotifyOrderStatus(customer, vehicle, order);
                succeeded = true;
            }
            catch (Exception exception)
            {
                Logger.LogError(
                    "Falha ao enviar notificação da ordem. EventType: {EventType}. OrderId: {OrderId}. ExceptionType: {ExceptionType}",
                    applicationEvent.GetType().Name,
                    order.OrderId,
                    exception.GetType().FullName);
            }
            finally
            {
                Metrics.RecordNotification(applicationEvent is BudgetAvailableEvent, succeeded);
            }
        }

        private static OrderNotificationSnapshot? GetOrder(IApplicationEvent applicationEvent) =>
            applicationEvent switch
            {
                BudgetAvailableEvent budgetAvailable => budgetAvailable.Order,
                OrderStatusChangedEvent statusChanged => statusChanged.Order,
                _ => null
            };
    }
}
