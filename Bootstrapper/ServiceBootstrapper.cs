using Microsoft.Extensions.DependencyInjection;
using Service;
using Service.Interface;

namespace Bootstrapper
{
    public static class ServiceBootstrapper
    {
        public static void Register(IServiceCollection service)
        {
            service.AddTransient<IClienteService, ClienteService>();
        }
    }
}
