using Infrastructure.Authentication;
using Infrastructure.Email;
using Service.Interface.Authentication;
using Service.Interface.Email;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection
{
    public static class InfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var emailSettings = configuration.GetSection("EmailSettings");
            services.Configure<EmailSettings>(settings =>
            {
                settings.Host = emailSettings["Host"] ?? string.Empty;
                settings.Port = int.TryParse(emailSettings["Port"], out var port) ? port : 0;
                settings.Username = emailSettings["Username"] ?? string.Empty;
                settings.Password = emailSettings["Password"] ?? string.Empty;
                settings.SenderName = emailSettings["SenderName"] ?? string.Empty;
                settings.SenderEmail = emailSettings["SenderEmail"] ?? string.Empty;
                settings.UseTls = bool.TryParse(emailSettings["UseTls"], out var useTls) && useTls;
            });

            services.AddTransient<ITokenGenerator, JwtTokenGenerator>();
            services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
            services.AddTransient<ISmtpClient, SmtpClient>();
            services.AddTransient<SmtpConnection>();
            services.AddTransient<IEmailSender, MailKitEmailSender>();

            return services;
        }
    }
}
