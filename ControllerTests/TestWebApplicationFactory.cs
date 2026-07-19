using GerenciamentoMecanicaSistema;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Service.Interface;

namespace ControllerTests
{
    public class TestWebApplicationFactory(string? databaseConnectionString = null) : WebApplicationFactory<Program>
    {
        public ICustomerService CustomerServiceMock { get; private set; } = Substitute.For<ICustomerService>();
        public IUserService UserServiceMock { get; private set; } = Substitute.For<IUserService>();
        public IAuthenticationService AuthenticationServiceMock { get; private set; } = Substitute.For<IAuthenticationService>();
        public IStockService StockServiceMock { get; private set; } = Substitute.For<IStockService>();
        public IVehicleService VehicleServiceMock { get; private set; } = Substitute.For<IVehicleService>();
        public ICatalogService MechanicalServiceMock { get; private set; } = Substitute.For<ICatalogService>();
        public IOrdersService OrderServiceMock { get; private set; } = Substitute.For<IOrdersService>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (!string.IsNullOrWhiteSpace(databaseConnectionString))
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = databaseConnectionString
                    });
                });
            }

            builder.ConfigureServices(services =>
            {
                List<ServiceDescriptor?> servicesToMock =
                [
                    services.SingleOrDefault(d => d.ServiceType == typeof(ICustomerService)),
                    services.SingleOrDefault(d => d.ServiceType == typeof(IUserService)),
                    services.SingleOrDefault(d => d.ServiceType == typeof(IAuthenticationService)),
                    services.SingleOrDefault(d => d.ServiceType == typeof(IStockService)),
                    services.SingleOrDefault(d => d.ServiceType == typeof(IVehicleService)),
                    services.SingleOrDefault(d => d.ServiceType == typeof(ICatalogService)),
                    services.SingleOrDefault(d => d.ServiceType == typeof(IOrdersService)),
                ];

                foreach (ServiceDescriptor? serviceDescriptor in servicesToMock)
                    if (serviceDescriptor != null)
                        services.Remove(serviceDescriptor);

                services.AddSingleton(CustomerServiceMock);
                services.AddSingleton(UserServiceMock);
                services.AddSingleton(AuthenticationServiceMock);
                services.AddSingleton(StockServiceMock);
                services.AddSingleton(VehicleServiceMock);
                services.AddSingleton(MechanicalServiceMock);
                services.AddSingleton(OrderServiceMock);
            });
        }
    }
}
