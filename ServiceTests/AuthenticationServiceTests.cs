using Domain.User;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto.User;

namespace ServiceTests
{
    public class AuthenticationServiceTests
    {
        private IAuthenticationService AuthenticationService { get; set; }
        private IUserRepository UsuarioRepository { get; set; }
        private IConfiguration Configuration { get; set; }

        private static readonly UserDto UsuarioExistente = new(Guid.NewGuid(), "Admin", "Admin@123", "Admin");
        private static readonly UserDto UsuarioExistenteComSenhaErrada = new(Guid.NewGuid(), "Admin", "Teste@123", "Admin");
        private static readonly UserDto UsuarioInexistente = new(Guid.NewGuid(), "Fulano", "Fulano@123", "Usuario");

        [SetUp]
        public void SetUp()
        {
            UsuarioRepository = Substitute.For<IUserRepository>();
            Configuration = Substitute.For<IConfiguration>();

            UsuarioRepository.GetUser(UsuarioExistente.Name, UsuarioExistente.Role).Returns(new User(UsuarioExistente.Name, UsuarioExistente.Password, UsuarioExistente.Role));

            Configuration["Jwt:Key"].Returns("chaveTestesecurityKeyfortestingTokengeneration");
            Configuration["Jwt:Issuer"].Returns("admin");
            Configuration["Jwt:Audience"].Returns("mecanica");

            AuthenticationService = new AuthenticationService(Configuration, UsuarioRepository);
        }

        [Test]
        public async Task MustLogInAndGenerateToken()
        {
            var token = await AuthenticationService.Authenticate(UsuarioExistente);

            Assert.That(string.IsNullOrEmpty(token), Is.False);

            await UsuarioRepository.Received(1).GetUser(UsuarioExistente.Name, UsuarioExistente.Role);
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioNotExists()
        {
            var token = await AuthenticationService.Authenticate(UsuarioInexistente);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioRepository.Received(1).GetUser(UsuarioInexistente.Name, UsuarioInexistente.Role);
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioTypedWrongPassword()
        {
            var token = await AuthenticationService.Authenticate(UsuarioExistenteComSenhaErrada);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioRepository.Received(1).GetUser(UsuarioExistenteComSenhaErrada.Name, UsuarioExistenteComSenhaErrada.Role);
        }
    }
}
