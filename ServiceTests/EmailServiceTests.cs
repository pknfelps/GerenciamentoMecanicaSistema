using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Vehicle;
using Service.Interface.Email;
using NSubstitute;
using Service;
using Service.Interface;
using Service.Interface.Events.Order;

namespace ServiceTests
{
    public class EmailServiceTests
    {
        private IEmailSender EmailSender { get; set; }
        private IEmailService EmailService { get; set; }

        [SetUp]
        public void SetUp()
        {
            EmailSender = Substitute.For<IEmailSender>();
            EmailService = new EmailService(EmailSender);
        }

        [Test]
        public async Task MustNotifyBudget()
        {
            var customer = Substitute.For<ICustomer>();
            customer.Name.Returns("Teste");
            customer.Email.Address.Returns("teste@gmail.com");

            var vehicle = Substitute.For<IVehicle>();
            vehicle.Model.Returns("Civic");
            vehicle.LicensePlate.License.Returns("CVC2026");

            var order = new OrderNotificationSnapshot(
                Guid.NewGuid(),
                "41738422011",
                "CVC2026",
                100m,
                WorkOrderStatus.WaitingForApproval);

            await EmailService.NotifyBudget(customer, vehicle, order);

            await EmailSender.Received(1).SendAsync(
                customer.Name,
                customer.Email.Address,
                "Serviço Finalizado",
                Arg.Is<string>(body => body.Contains(customer.Name) && body.Contains(vehicle.Model) && body.Contains(order.Budget.ToString())),
                Arg.Is<string>(body => body.Contains(customer.Name) && body.Contains(vehicle.Model) && body.Contains(order.Budget.ToString())));
        }

        [Test]
        public async Task MustNotifyOrderStatus()
        {
            var customer = Substitute.For<ICustomer>();
            customer.Name.Returns("Teste");
            customer.Email.Address.Returns("teste@gmail.com");

            var vehicle = Substitute.For<IVehicle>();
            vehicle.Model.Returns("Civic");
            vehicle.LicensePlate.License.Returns("CVC2026");

            var order = new OrderNotificationSnapshot(
                Guid.NewGuid(),
                "41738422011",
                "CVC2026",
                100m,
                WorkOrderStatus.InExecution);

            await EmailService.NotifyOrderStatus(customer, vehicle, order);

            await EmailSender.Received(1).SendAsync(
                customer.Name,
                customer.Email.Address,
                Arg.Is<string>(subject => subject.Contains(order.OrderId.ToString())),
                Arg.Is<string>(body => body.Contains(customer.Name) && body.Contains(vehicle.Model) && body.Contains("em execução")),
                Arg.Is<string>(body => body.Contains(customer.Name) && body.Contains(vehicle.Model) && body.Contains("em execução")));
        }
    }
}
