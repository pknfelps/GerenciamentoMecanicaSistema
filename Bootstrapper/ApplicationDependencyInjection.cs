using Microsoft.Extensions.DependencyInjection;
using Service;
using Service.Events;
using Service.Interface;
using Service.Interface.Events;

namespace DependencyInjection
{
    public static class ApplicationDependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddTransient<ICustomerService, CustomerService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IAuthenticationService, AuthenticationService>();
            services.AddTransient<IStockService, StockService>();
            services.AddTransient<IVehicleService, VehicleService>();
            services.AddTransient<ICatalogService, CatalogService>();
            services.AddTransient<IOrdersService, OrdersService>();
            services.AddTransient<IOrderDependenciesGateway, OrderDependenciesGateway>();
            services.AddTransient<IApplicationEventDispatcher, ApplicationEventDispatcher>();
            services.AddTransient<IApplicationEventHandler, OrderNotificationEventHandler>();
            services.AddTransient<IEmailService, EmailService>();

            return services;
        }
    }
}
