using Microsoft.Extensions.DependencyInjection;
using Service;
using Service.Interface;

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
            service.AddTransient<IMechanicalServiceService, MechanicalServiceService>();
            service.AddTransient<IWorkOrderService, WorkOrderService>();
        }
    }
}
