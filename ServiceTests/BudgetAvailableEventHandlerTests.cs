using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Vehicle;
using NSubstitute;
using Service.Events;
using Service.Interface;
using Service.Interface.Events.Order;

namespace ServiceTests
{
    public class BudgetAvailableEventHandlerTests
    {
        private IOrderDependenciesGateway DependenciesGateway { get; set; }
        private IEmailService EmailService { get; set; }
        private BudgetAvailableEventHandler Handler { get; set; }

        [SetUp]
        public void SetUp()
        {
            DependenciesGateway = Substitute.For<IOrderDependenciesGateway>();
            EmailService = Substitute.For<IEmailService>();
            Handler = new BudgetAvailableEventHandler(DependenciesGateway, EmailService);
        }

        [Test]
        public async Task MustNotifyBudget()
        {
            var order = CreateOrder();
            var customer = Substitute.For<ICustomer>();
            var vehicle = Substitute.For<IVehicle>();

            DependenciesGateway.GetCustomerByDocument(order.CustomerDocument.Id).Returns(customer);
            DependenciesGateway.GetVehicleByLicensePlate(order.VehicleLicensePlate.License).Returns(vehicle);
            EmailService.NotifyBudget(customer, vehicle, order).Returns(Task.CompletedTask);

            await Handler.Handle(new BudgetAvailableEvent(order));

            await DependenciesGateway.Received(1).GetCustomerByDocument(order.CustomerDocument.Id);
            await DependenciesGateway.Received(1).GetVehicleByLicensePlate(order.VehicleLicensePlate.License);
            await EmailService.Received(1).NotifyBudget(customer, vehicle, order);
        }

        [Test]
        public async Task MustNotPropagateNotificationFailure()
        {
            var order = CreateOrder();

            DependenciesGateway.GetCustomerByDocument(order.CustomerDocument.Id).Returns((ICustomer?)null);

            Assert.DoesNotThrowAsync(async () => await Handler.Handle(new BudgetAvailableEvent(order)));

            await DependenciesGateway.Received(1).GetCustomerByDocument(order.CustomerDocument.Id);
            await DependenciesGateway.ReceivedWithAnyArgs(0).GetVehicleByLicensePlate(default!);
            await EmailService.ReceivedWithAnyArgs(0).NotifyBudget(default!, default!, default!);
        }

        private static IOrder CreateOrder()
        {
            var order = Substitute.For<IOrder>();
            order.CustomerDocument.Id.Returns("41738422011");
            order.VehicleLicensePlate.License.Returns("CVC2026");

            return order;
        }
    }
}
