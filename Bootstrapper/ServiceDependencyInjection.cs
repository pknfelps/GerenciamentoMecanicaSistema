using Infrastructure.Authentication;
using Infrastructure.Email;
using MailKit.Net.Smtp;
using Microsoft.Extensions.DependencyInjection;
using Service;
using Service.Events;
using Service.Interface;
using Service.Interface.Events;

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
            service.AddTransient<IOrdersService, OrdersService>();
            service.AddTransient<IOrderDependenciesGateway, OrderDependenciesGateway>();
            service.AddTransient<IApplicationEventDispatcher, ApplicationEventDispatcher>();
            service.AddTransient<IApplicationEventHandler, BudgetAvailableEventHandler>();
            service.AddTransient<ITokenGenerator, JwtTokenGenerator>();

            service.AddTransient<ISmtpClient, SmtpClient>();
            service.AddTransient<SmtpConnection>();
            service.AddTransient<IEmailSender, MailKitEmailSender>();
            service.AddTransient<IEmailService, EmailService>();
        }
    }
}
