using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using NSubstitute;
using Service.Interface;

namespace ControllerTests
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        public IClienteService ClienteServiceMock { get; private set; } = Substitute.For<IClienteService>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove o registro real e substitui pelo mock
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IClienteService));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddSingleton(ClienteServiceMock);
            });
        }
    }
}
