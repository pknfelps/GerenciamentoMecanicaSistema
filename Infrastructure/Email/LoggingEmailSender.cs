using Microsoft.Extensions.Logging;
using Service.Interface.Email;

namespace Infrastructure.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toName, string toEmail, string subject, string textBody, string htmlBody)
    {
        logger.LogInformation("Envio de e-mail simulado.");
        return Task.CompletedTask;
    }
}
