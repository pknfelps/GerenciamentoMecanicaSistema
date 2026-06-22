using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service;
using Service.Interface;
using Service.Settings;

namespace DependencyInjection
{
    public static class ServiceDependencyInjection
    {
        public static void Register(IServiceCollection service)
        {
            service.AddTransient<ICustomerService, CustomerService>();
            service.AddTransient<IUserService, UserService>();
            service.AddTransient<IAuthenticationService, AuthenticationService>();
            service.AddTransient<IStockService, StockService>();
            service.AddTransient<IVehicleService, VehicleService>();
            service.AddTransient<ICatalogService, CatalogService>();
            service.AddTransient<IWorkOrderService, OrdersService>();

            service.AddTransient<ISmtpClient, SmtpClient>();
            service.AddTransient<ISmtpConnection, SmtpConnection>();
            service.AddTransient<IEmailService, EmailService>();
        }
    }
}
