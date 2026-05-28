using Microsoft.Extensions.Configuration;
using NSubstitute;
using Service;
using Service.Interface;
using Service.Interface.Dto;

namespace ServiceTests
{
    public class AuthenticationServiceTests
    {
        private IAuthenticationService AuthenticationService { get; set; }
        private IUsuarioService UsuarioService { get; set; }
        private IConfiguration Configuration { get; set; }

        private static readonly UsuarioDto UsuarioExistente = new("Admin", "Admin@123", "Admin");
        private static readonly UsuarioDto UsuarioExistenteComSenhaErrada = new("Admin", "Teste@123", "Admin");
        private static readonly UsuarioDto UsuarioInexistente = new("Fulano", "Fulano@123", "Usuario");

        [SetUp]
        public void SetUp()
        {
            UsuarioService = Substitute.For<IUsuarioService>();
            Configuration = Substitute.For<IConfiguration>();

            UsuarioService.GetUsuario(Arg.Any<UsuarioDto>()).Returns(callInfo =>
            {
                var usuario = callInfo.ArgAt<UsuarioDto>(0);

                if (usuario.Nome == UsuarioExistente.Nome && usuario.Cargo == UsuarioExistente.Cargo)
                    return UsuarioExistente;

                return null;
            });

            Configuration["Jwt:Key"].Returns("chaveTestesecurityKeyfortestingTokengeneration");
            Configuration["Jwt:Issuer"].Returns("admin");
            Configuration["Jwt:Audience"].Returns("mecanica");

            AuthenticationService = new AuthenticationService(Configuration, UsuarioService);
        }

        [Test]
        public async Task MustLogInAndGenerateToken()
        {
            var token = await AuthenticationService.Login(UsuarioExistente);

            Assert.That(string.IsNullOrEmpty(token), Is.False);

            await UsuarioService.Received(1).GetUsuario(UsuarioExistente);
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioNotExists()
        {
            var token = await AuthenticationService.Login(UsuarioInexistente);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioService.Received(1).GetUsuario(UsuarioInexistente);
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioTypedWrongPassword()
        {
            var token = await AuthenticationService.Login(UsuarioExistenteComSenhaErrada);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioService.Received(1).GetUsuario(UsuarioExistenteComSenhaErrada);
        }
    }
}
