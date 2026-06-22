using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Vehicle;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using Service;
using Service.Interface;
using Service.Settings;

namespace ServiceTests
{
    public class EmailServiceTests
    {
        private ISmtpClient Client { get; set; }
        private ISmtpConnection Connection { get; set; }
        private IEmailService EmailService { get; set; }

        [SetUp]
        public void SetUp()
        {
            EmailSettings emailSettings = new() { SenderName = "Mecânica", SenderEmail = "noreply@mecanica.com" };

            var settings = Substitute.For<IOptions<EmailSettings>>();
            settings.Value.Returns(emailSettings);

            Client = Substitute.For<ISmtpClient>();
            Client.SendAsync(Arg.Any<MimeMessage>()).Returns("completed");

            Connection = Substitute.For<ISmtpConnection>();
            Connection.GetClientAsync().Returns(Client);

            EmailService = new EmailService(settings, Connection);
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

            await Connection.Received(1).GetClientAsync();
            await Client.ReceivedWithAnyArgs(1).SendAsync(Arg.Any<MimeMessage>());
        }

        [TearDown]
        public void TearDown()
        {
            Client.Dispose();
        }
    }
}
