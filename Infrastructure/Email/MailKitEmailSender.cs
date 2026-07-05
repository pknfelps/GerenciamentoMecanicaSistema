using Microsoft.Extensions.Options;
using MimeKit;
using Service.Interface;

namespace Infrastructure.Email
{
    public class MailKitEmailSender(IOptions<EmailSettings> settings, SmtpConnection connection) : IEmailSender
    {
        private EmailSettings Settings { get; set; } = settings.Value;
        private SmtpConnection Connection { get; set; } = connection;

        public async Task SendAsync(string toName, string toEmail, string subject, string textBody, string htmlBody)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(Settings.SenderName, Settings.SenderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                TextBody = textBody,
                HtmlBody = htmlBody
            };

            message.Body = bodyBuilder.ToMessageBody();

            var client = await Connection.GetClientAsync();
            await client.SendAsync(message);
        }
    }
}
