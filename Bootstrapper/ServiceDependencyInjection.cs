using Microsoft.Extensions.DependencyInjection;
using Service;
using Service.Interface;

namespace DependencyInjection
{
    public static class ServiceDependencyInjection
    {
        public static void Register(IServiceCollection service)
        {
            service.AddTransient<IClienteService, ClienteService>();
            service.AddTransient<IUsuarioService, UsuarioService>();
            service.AddTransient<IAuthenticationService, AuthenticationService>();
            service.AddTransient<IStockService, StockService>();
            service.AddTransient<IVehicleService, VehicleService>();
        }
    }
}
