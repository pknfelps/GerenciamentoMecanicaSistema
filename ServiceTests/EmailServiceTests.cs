using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Vehicle;
using NSubstitute;
using Service;
using Service.Interface;

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

            var order = Substitute.For<IOrder>();
            order.Budget.Returns(100);
            order.Id.Returns(Guid.NewGuid());

            await EmailService.NotifyBudget(customer, vehicle, order);

            await EmailSender.Received(1).SendAsync(
                customer.Name,
                customer.Email.Address,
                "Serviço Finalizado",
                Arg.Is<string>(body => body.Contains(customer.Name) && body.Contains(vehicle.Model) && body.Contains(order.Budget.ToString())),
                Arg.Is<string>(body => body.Contains(customer.Name) && body.Contains(vehicle.Model) && body.Contains(order.Budget.ToString())));
        }
    }
}
