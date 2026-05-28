using NSubstitute;
using Service;
using Service.Interface;
using Service.Interface.Dto;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class AuthenticationControllerTests : BaseControllerTests
    {
        private IAuthenticationService AuthenticationService { get; set; }

        private readonly UsuarioDto UsuarioExistente = new("Ciclano", "Ciclano@123", "Admin");
        private readonly UsuarioDto UsuarioExistenteComSenhaErrada = new("Ciclano", "Ciclano@321", "Admin");
        private readonly UsuarioDto UsuarioInexistente = new("Fulano", "Fulano@123", "Usuario");
        private readonly string TokenValido = "TokenvalidoCriadocomsUcessoaPartirDetestedeController";
        private readonly string UnauthorizedMessage = "Usuário ou senha inválidos";

        public override void SetUp()
        {
            TestWebAppFactory = new TestWebApplicationFactory();
            TestClient = TestWebAppFactory.CreateClient();

            AuthenticationService = TestWebAppFactory.AuthenticationServiceMock;

            AuthenticationService.Login(Arg.Any<UsuarioDto>()).Returns(callInfo =>
            {
                var usuario = callInfo.Arg<UsuarioDto>();

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

            await AuthenticationService.Received(1).Login(UsuarioExistente);

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

            await AuthenticationService.Received(1).Login(UsuarioInexistente);

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

            await AuthenticationService.Received(1).Login(UsuarioExistenteComSenhaErrada);

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

            await AuthenticationService.ReceivedWithAnyArgs(0).Login(Arg.Any<UsuarioDto>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}
