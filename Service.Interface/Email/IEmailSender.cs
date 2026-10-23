namespace Service.Interface.Email
{
    public interface IEmailSender
    {
        Task SendAsync(string type, string toName, string toEmail, string subject, string textBody, string htmlBody, CancellationToken cancellationToken = default);
    }
}
