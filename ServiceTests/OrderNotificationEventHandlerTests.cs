using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Vehicle;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Service.Events;
using Service.Interface;
using Service.Interface.Events.Order;

namespace ServiceTests
{
    public class OrderNotificationEventHandlerTests
    {
        private IOrderDependenciesGateway DependenciesGateway { get; set; }
        private IEmailService EmailService { get; set; }
        private OrderNotificationEventHandler Handler { get; set; }

        [SetUp]
        public void SetUp()
        {
            DependenciesGateway = Substitute.For<IOrderDependenciesGateway>();
            EmailService = Substitute.For<IEmailService>();
            Handler = new OrderNotificationEventHandler(
                DependenciesGateway,
                EmailService,
                Substitute.For<ILogger<OrderNotificationEventHandler>>());
        }

        [Test]
        public async Task MustNotifyBudget()
        {
            var order = CreateOrder();
            var customer = Substitute.For<ICustomer>();
            var vehicle = Substitute.For<IVehicle>();

            DependenciesGateway.GetCustomerByDocument(order.CustomerDocument).Returns(customer);
            DependenciesGateway.GetVehicleByLicensePlate(order.VehicleLicensePlate).Returns(vehicle);

            await Handler.Handle(new BudgetAvailableEvent(order));

            await DependenciesGateway.Received(1).GetCustomerByDocument(order.CustomerDocument);
            await DependenciesGateway.Received(1).GetVehicleByLicensePlate(order.VehicleLicensePlate);
            await EmailService.Received(1).NotifyBudget(customer, vehicle, order);
            await EmailService.ReceivedWithAnyArgs(0).NotifyOrderStatus(default!, default!, default!);
        }

        [Test]
        public async Task MustNotifyOrderStatus()
        {
            var order = CreateOrder();
            var customer = Substitute.For<ICustomer>();
            var vehicle = Substitute.For<IVehicle>();

            DependenciesGateway.GetCustomerByDocument(order.CustomerDocument).Returns(customer);
            DependenciesGateway.GetVehicleByLicensePlate(order.VehicleLicensePlate).Returns(vehicle);

            await Handler.Handle(new OrderStatusChangedEvent(order));

            await DependenciesGateway.Received(1).GetCustomerByDocument(order.CustomerDocument);
            await DependenciesGateway.Received(1).GetVehicleByLicensePlate(order.VehicleLicensePlate);
            await EmailService.Received(1).NotifyOrderStatus(customer, vehicle, order);
            await EmailService.ReceivedWithAnyArgs(0).NotifyBudget(default!, default!, default!);
        }

        [Test]
        public async Task MustNotPropagateNotificationFailure()
        {
            var order = CreateOrder();

            DependenciesGateway.GetCustomerByDocument(order.CustomerDocument).Returns((ICustomer?)null);

            Assert.DoesNotThrowAsync(async () => await Handler.Handle(new OrderStatusChangedEvent(order)));

            await DependenciesGateway.Received(1).GetCustomerByDocument(order.CustomerDocument);
            await DependenciesGateway.ReceivedWithAnyArgs(0).GetVehicleByLicensePlate(default!);
            await EmailService.ReceivedWithAnyArgs(0).NotifyOrderStatus(default!, default!, default!);
        }

        private static OrderNotificationSnapshot CreateOrder() =>
            new(Guid.NewGuid(), "41738422011", "CVC2026", 100m, WorkOrderStatus.WaitingForApproval);
    }
}
