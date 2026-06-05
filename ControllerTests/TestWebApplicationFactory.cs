using GerenciamentoMecanicaSistema;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Service.Interface;

namespace ControllerTests
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        public IClienteService ClienteServiceMock { get; private set; } = Substitute.For<IClienteService>();
        public IUsuarioService UsuarioServiceMock { get; private set; } = Substitute.For<IUsuarioService>();
        public IAuthenticationService AuthenticationServiceMock { get; private set; } = Substitute.For<IAuthenticationService>();
        public IStockService StockServiceMock { get; private set; } = Substitute.For<IStockService>();
        public IVehicleService VehicleServiceMock { get; private set; } = Substitute.For<IVehicleService>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                List<ServiceDescriptor?> servicesToMock =
                [
                    services.SingleOrDefault(d => d.ServiceType == typeof(IClienteService)),
                    services.SingleOrDefault(d => d.ServiceType == typeof(IUsuarioService)),
                    services.SingleOrDefault(d => d.ServiceType == typeof(IAuthenticationService)),
                    services.SingleOrDefault(d => d.ServiceType == typeof(IStockService)),
                    services.SingleOrDefault(d => d.ServiceType == typeof(IVehicleService)),
                ];

                foreach (ServiceDescriptor? serviceDescriptor in servicesToMock)
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                services.AddSingleton(ClienteServiceMock);
                services.AddSingleton(UsuarioServiceMock);
                services.AddSingleton(AuthenticationServiceMock);
                services.AddSingleton(StockServiceMock);
                services.AddSingleton(VehicleServiceMock);
            });
        }
    }
}
