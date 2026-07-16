namespace Service.Interface
{
    public interface IEmailSender
    {
        Task SendAsync(string toName, string toEmail, string subject, string textBody, string htmlBody);
    }
}
