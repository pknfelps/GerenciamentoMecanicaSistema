using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Vehicle;
using Microsoft.Extensions.Options;
using MimeKit;
using Service.Interface;
using Service.Settings;

namespace Service
{
    public class EmailService(IOptions<EmailSettings> settings, ISmtpConnection connection) : IEmailService
    {
        private EmailSettings Settings { get; set; } = settings.Value;
        private ISmtpConnection Connection { get; set; } = connection;

        public async Task NotifyBudget(ICustomer customer, IVehicle vehicle, IOrder order)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(Settings.SenderName, Settings.SenderEmail));
            message.To.Add(new MailboxAddress(customer.Name, customer.Email.Address));
            message.Subject = "Serviço Finalizado";

            var bodyBuilder = new BodyBuilder
            {
                TextBody = $"""
                Olá {customer.Name}. Gostaríamos de informar que o orçamento do serviço no seu {vehicle.Model} com a placa {vehicle.LicensePlate.License} ficou no valor de R${order.Budget}.

                Atenciosamente, Mecânica.
                """,

                HtmlBody = $"""
                Olá {customer.Name}. Gostaríamos de informar que o orçamento do serviço no seu {vehicle.Model} com a placa {vehicle.LicensePlate.License} ficou no valor de R${order.Budget}.

                Atenciosamente, Mecânica.
                """
            };

            message.Body = bodyBuilder.ToMessageBody();

            var client = await Connection.GetClientAsync();
            await client.SendAsync(message);
        }
    }
}
