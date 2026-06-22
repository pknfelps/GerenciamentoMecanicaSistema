using MailKit.Net.Smtp;

namespace Service.Interface
{
    public interface ISmtpConnection
    {
        Task<ISmtpClient> GetClientAsync();
    }
}
