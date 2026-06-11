using NSubstitute;
using Service;
using Service.Interface;
using Service.Interface.Dto.User;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class AuthenticationControllerTests : BaseControllerTests
    {
        private IAuthenticationService AuthenticationService { get; set; }

        private readonly UserDto UsuarioExistente = new(Guid.NewGuid(), "Ciclano", "Ciclano@123", "Admin");
        private readonly UserDto UsuarioExistenteComSenhaErrada = new(Guid.NewGuid(), "Ciclano", "Ciclano@321", "Admin");
        private readonly UserDto UsuarioInexistente = new(Guid.NewGuid(), "Fulano", "Fulano@123", "Usuario");
        private readonly string TokenValido = "TokenvalidoCriadocomsUcessoaPartirDetestedeController";
        private readonly string UnauthorizedMessage = "Usuário ou senha inválidos";

        public override void SetUp()
        {
            TestWebAppFactory = new TestWebApplicationFactory();
            TestClient = TestWebAppFactory.CreateClient();

            MockService();
        }

        protected override void MockService()
        {
            AuthenticationService = TestWebAppFactory.AuthenticationServiceMock;

            AuthenticationService.Authenticate(Arg.Any<UserDto>()).Returns(callInfo =>
            {
                var usuario = callInfo.Arg<UserDto>();

                if (usuario.Equals(UsuarioExistente))
                    return TokenValido;

                return string.Empty;
            });
        }

        [Test]
        public async Task MustLoginAndGetToken()
        {
            var response = await TestClient.PostAsJsonAsync("/Authentication/Login", UsuarioExistente);

            var token = await response.Content.ReadAsStringAsync();

            await AuthenticationService.Received(1).Authenticate(UsuarioExistente);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(token, Is.Not.Null);
                Assert.That(token, Is.Not.Empty);
                Assert.That(token, Is.EqualTo(TokenValido));
            });
        }

        [Test]
        public async Task MustNotLoginAndNotGetTokenIfUsuarioNotExists()
        {
            var response = await TestClient.PostAsJsonAsync("/Authentication/Login", UsuarioInexistente);

            var token = await response.Content.ReadAsStringAsync();

            await AuthenticationService.Received(1).Authenticate(UsuarioInexistente);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(token, Is.EqualTo(UnauthorizedMessage));
            });
        }

        [Test]
        public async Task MustNotLoginAndNotGetTokenIfPasswordIsWrong()
        {
            var response = await TestClient.PostAsJsonAsync("/Authentication/Login", UsuarioExistenteComSenhaErrada);

            var token = await response.Content.ReadAsStringAsync();

            await AuthenticationService.Received(1).Authenticate(UsuarioExistenteComSenhaErrada);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(token, Is.EqualTo(UnauthorizedMessage));
            });
        }

        [Test]
        public async Task MustReturnBadRequestIfModelIsInvalid()
        {
            var response = await TestClient.PostAsJsonAsync("/Authentication/Login", new { Nome = "Teste", Cargo = "Inválido" });

            await AuthenticationService.ReceivedWithAnyArgs(0).Authenticate(Arg.Any<UserDto>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}
