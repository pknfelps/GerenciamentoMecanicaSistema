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
        private ILogger<OrderNotificationEventHandler> Logger { get; set; }

        [SetUp]
        public void SetUp()
        {
            DependenciesGateway = Substitute.For<IOrderDependenciesGateway>();
            EmailService = Substitute.For<IEmailService>();
            Logger = Substitute.For<ILogger<OrderNotificationEventHandler>>();
            Handler = new OrderNotificationEventHandler(
                DependenciesGateway,
                EmailService,
                Logger);
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

        [TestCase(true)]
        [TestCase(false)]
        public async Task MustLogOnlyTechnicalNotificationDetails(bool budget)
        {
            var order = CreateOrder();
            var customer = Substitute.For<ICustomer>();
            var vehicle = Substitute.For<IVehicle>();
            DependenciesGateway.GetCustomerByDocument(order.CustomerDocument).Returns(customer);
            DependenciesGateway.GetVehicleByLicensePlate(order.VehicleLicensePlate).Returns(vehicle);
            EmailService.NotifyBudget(customer, vehicle, order).Returns(_ => ThrowNotificationFailure());
            EmailService.NotifyOrderStatus(customer, vehicle, order).Returns(_ => ThrowNotificationFailure());

            await Handler.Handle(budget ? new BudgetAvailableEvent(order) : new OrderStatusChangedEvent(order));

            var call = Logger.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(ILogger.Log));
            var arguments = call.GetArguments();
            var properties = ((IEnumerable<KeyValuePair<string, object?>>)arguments[2]!).ToDictionary(pair => pair.Key, pair => pair.Value);
            Assert.Multiple(() =>
            {
                Assert.That(arguments[0], Is.EqualTo(LogLevel.Error));
                Assert.That(arguments[3], Is.Null);
                Assert.That(properties["OrderId"], Is.EqualTo(order.OrderId));
                Assert.That(properties["ExceptionType"], Is.EqualTo(typeof(InvalidOperationException).FullName));
                Assert.That(properties.Keys, Is.EquivalentTo(new[] { "EventType", "OrderId", "ExceptionType", "{OriginalFormat}" }));
                Assert.That(arguments[2]!.ToString(), Does.Not.Contain(nameof(ThrowNotificationFailure)));
                foreach (var value in new[] { order.CustomerDocument, order.VehicleLicensePlate, "email@example.invalid", "private-message", "private-inner", "private-data", "123456.78" })
                {
                    Assert.That(arguments[2]!.ToString(), Does.Not.Contain(value));
                    Assert.That(string.Join(" ", properties.Values), Does.Not.Contain(value));
                }
            });
        }

        private static async Task ThrowNotificationFailure()
        {
            await Task.Yield();
            var error = new InvalidOperationException("private-message email@example.invalid 123456.78", new Exception("private-inner"));
            error.Data["detail"] = "private-data";
            throw error;
        }

        private static OrderNotificationSnapshot CreateOrder() =>
            new(Guid.NewGuid(), "41738422011", "CVC2026", 100m, WorkOrderStatus.WaitingForApproval);
    }
}
