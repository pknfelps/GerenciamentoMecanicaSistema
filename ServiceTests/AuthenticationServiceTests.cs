using Domain.Interface.User;
using Domain.User;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto;

namespace ServiceTests
{
    public class AuthenticationServiceTests
    {
        private IAuthenticationService AuthenticationService { get; set; }
        private IUsuarioRepository UsuarioRepository { get; set; }
        private IConfiguration Configuration { get; set; }

        private static readonly UsuarioDto UsuarioExistente = new("Admin", "Admin@123", "Admin");
        private static readonly UsuarioDto UsuarioExistenteComSenhaErrada = new("Admin", "Teste@123", "Admin");
        private static readonly UsuarioDto UsuarioInexistente = new("Fulano", "Fulano@123", "Usuario");

        [SetUp]
        public void SetUp()
        {
            UsuarioRepository = Substitute.For<IUsuarioRepository>();
            Configuration = Substitute.For<IConfiguration>();

            UsuarioRepository.GetUsuarioByNomeAndCargo(UsuarioExistente.Nome, UsuarioExistente.Cargo).Returns(new Usuario(UsuarioExistente.Nome, UsuarioExistente.Senha, UsuarioExistente.Cargo));

            Configuration["Jwt:Key"].Returns("chaveTestesecurityKeyfortestingTokengeneration");
            Configuration["Jwt:Issuer"].Returns("admin");
            Configuration["Jwt:Audience"].Returns("mecanica");

            AuthenticationService = new AuthenticationService(Configuration, UsuarioRepository);
        }

        [Test]
        public async Task MustLogInAndGenerateToken()
        {
            var token = await AuthenticationService.Login(UsuarioExistente);

            Assert.That(string.IsNullOrEmpty(token), Is.False);

            await UsuarioRepository.Received(1).GetUsuarioByNomeAndCargo(UsuarioExistente.Nome, UsuarioExistente.Cargo);
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioNotExists()
        {
            var token = await AuthenticationService.Login(UsuarioInexistente);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioRepository.Received(1).GetUsuarioByNomeAndCargo(UsuarioInexistente.Nome, UsuarioInexistente.Cargo);
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioTypedWrongPassword()
        {
            var token = await AuthenticationService.Login(UsuarioExistenteComSenhaErrada);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioRepository.Received(1).GetUsuarioByNomeAndCargo(UsuarioExistenteComSenhaErrada.Nome, UsuarioExistenteComSenhaErrada.Cargo);
        }
    }
}
