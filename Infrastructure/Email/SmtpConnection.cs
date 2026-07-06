using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email
{
    public class SmtpConnection(ISmtpClient client, IOptions<EmailSettings> settings, ILogger<SmtpConnection> logger) : IAsyncDisposable
    {
        private readonly ISmtpClient Client = client;
        private readonly EmailSettings Settings = settings.Value;
        private readonly ILogger<SmtpConnection> Logger = logger;

        public async Task<ISmtpClient> GetClientAsync()
        {
            if (Client.IsConnected)
                return Client;

            var socketOptions = Settings.UseTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            try
            {
                Logger.LogDebug("Conectando ao SMTP. Host: {Host}. Port: {Port}. UseTls: {UseTls}", Settings.Host, Settings.Port, Settings.UseTls);

                await Client.ConnectAsync(Settings.Host, Settings.Port, socketOptions);

                if (!string.IsNullOrEmpty(Settings.Username))
                    await Client.AuthenticateAsync(Settings.Username, Settings.Password);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Falha ao conectar ou autenticar no SMTP. Host: {Host}. Port: {Port}. UseTls: {UseTls}", Settings.Host, Settings.Port, Settings.UseTls);
                throw;
            }

            return Client;
        }

        public async ValueTask DisposeAsync()
        {
            if (Client.IsConnected)
                await Client.DisconnectAsync(true);

            Client.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
