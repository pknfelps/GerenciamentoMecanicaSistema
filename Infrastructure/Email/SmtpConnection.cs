using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email
{
    public class SmtpConnection(ISmtpClient client, IOptions<EmailSettings> settings) : IAsyncDisposable
    {
        private readonly ISmtpClient Client = client;
        private readonly EmailSettings Settings = settings.Value;

        public async Task<ISmtpClient> GetClientAsync()
        {
            if (Client.IsConnected)
                return Client;

            var socketOptions = Settings.UseTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await Client.ConnectAsync(Settings.Host, Settings.Port, socketOptions);

            if (!string.IsNullOrEmpty(Settings.Username))
                await Client.AuthenticateAsync(Settings.Username, Settings.Password);

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
