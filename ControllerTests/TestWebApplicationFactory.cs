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
                services.AddSingleton(UsuarioServiceMock);
                services.AddSingleton(AuthenticationServiceMock);
            });
        }
    }
}
