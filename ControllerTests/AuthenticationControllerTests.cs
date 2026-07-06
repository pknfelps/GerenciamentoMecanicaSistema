using GerenciamentoMecanicaSistema.Contracts.Requests.User;
using NSubstitute;
using Service.Interface;
using Service.Interface.Commands.User;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class AuthenticationControllerTests : BaseControllerTests
    {
        private IAuthenticationService AuthenticationService { get; set; }

        private readonly CreateUserRequest UsuarioExistente = new("Ciclano", "Ciclano@123", "Admin");
        private readonly CreateUserRequest UsuarioExistenteComSenhaErrada = new("Ciclano", "Ciclano@321", "Admin");
        private readonly CreateUserRequest UsuarioInexistente = new("Fulano", "Fulano@123", "Usuario");
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

            AuthenticationService.Authenticate(Arg.Any<CreateUserCommand>()).Returns(callInfo =>
            {
                var usuario = callInfo.Arg<CreateUserCommand>();

                if (usuario.Equals(UsuarioExistente.ToCommand()))
                    return TokenValido;

                return string.Empty;
            });
        }

        [Test]
        public async Task MustLoginAndGetToken()
        {
            var response = await TestClient.PostAsJsonAsync("authentication", UsuarioExistente);

            var token = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(token, Is.Not.Null);
                Assert.That(token, Is.Not.Empty);
                Assert.That(token, Is.EqualTo(TokenValido));
            });

            await AuthenticationService.Received(1).Authenticate(UsuarioExistente.ToCommand());
        }

        [Test]
        public async Task MustNotLoginAndNotGetTokenIfUsuarioNotExists()
        {
            var response = await TestClient.PostAsJsonAsync("authentication", UsuarioInexistente);

            var token = await response.Content.ReadAsStringAsync();

            await AuthenticationService.Received(1).Authenticate(UsuarioInexistente.ToCommand());

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(token, Is.EqualTo(UnauthorizedMessage));
            });
        }

        [Test]
        public async Task MustNotLoginAndNotGetTokenIfPasswordIsWrong()
        {
            var response = await TestClient.PostAsJsonAsync("authentication", UsuarioExistenteComSenhaErrada);

            var token = await response.Content.ReadAsStringAsync();

            await AuthenticationService.Received(1).Authenticate(UsuarioExistenteComSenhaErrada.ToCommand());

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(token, Is.EqualTo(UnauthorizedMessage));
            });
        }

        [Test]
        public async Task MustReturnBadRequestIfModelIsInvalid()
        {
            var response = await TestClient.PostAsJsonAsync("authentication", new { Nome = "Teste", Cargo = "Inválido" });

            await AuthenticationService.ReceivedWithAnyArgs(0).Authenticate(Arg.Any<CreateUserCommand>());

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}
