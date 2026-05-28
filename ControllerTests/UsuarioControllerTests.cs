using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Service.Interface;
using Service.Interface.Dto;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public partial class UsuarioControllerTests : BaseControllerTests
    {
        private IUsuarioService UsuarioService { get; set; }

        private readonly UsuarioDto UsuarioParaCriar = new("Fulano", "Fulano@123", "Usuario");
        private readonly UsuarioDto UsuarioExistente = new("Ciclano", "Ciclano@123", "Admin");
        private readonly UsuarioDto UsuarioExistenteSemSenha = new("Ciclano", "", "Admin");

        public override void SetUp()
        {
            base.SetUp();

            UsuarioService = TestWebAppFactory.UsuarioServiceMock;
        }

        [Test]
        public async Task MustRegisterUsuario()
        {
            UsuarioService.RegisterUsuario(UsuarioParaCriar).Returns(Task.CompletedTask);

            var response = await TestClient.PostAsJsonAsync("/Usuario/RegisterUsuario", UsuarioParaCriar);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await UsuarioService.Received(1).RegisterUsuario(UsuarioParaCriar);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRegisterAUsuarioThatIsNotValid()
        {
            var response = await TestClient.PostAsJsonAsync("/Usuario/RegisterUsuario", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await UsuarioService.ReceivedWithAnyArgs(0).RegisterUsuario(Arg.Any<UsuarioDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryRegisterAUsuarioThatAlreadyExists()
        {
            UsuarioService.RegisterUsuario(UsuarioExistente).Throws<InvalidOperationException>();

            var response = await TestClient.PostAsJsonAsync("/Usuario/RegisterUsuario", UsuarioExistente);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await UsuarioService.Received(1).RegisterUsuario(UsuarioExistente);
        }

        [Test]
        public async Task MustGetUsuarioByNomeAndCargo()
        {
            UsuarioService.GetUsuario(UsuarioExistenteSemSenha).Returns(UsuarioExistente);

            var response = await TestClient.GetAsync($"/Usuario/GetUsuarioByNomeAndCargo/{UsuarioExistenteSemSenha.Nome}/{UsuarioExistenteSemSenha.Cargo}");

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine(content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var usuario = await response.Content.ReadFromJsonAsync<UsuarioDto>();

            await UsuarioService.Received(1).GetUsuario(UsuarioExistenteSemSenha);

            Assert.That(usuario, Is.Not.Null);
            Assert.That(usuario.Equals(UsuarioExistente), Is.True);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryGetClienteClienteThatNotExists()
        {
            UsuarioService.GetUsuario(UsuarioParaCriar).Returns((UsuarioDto?)null);

            var response = await TestClient.GetAsync($"/Usuario/GetUsuarioByNomeAndCargo/{UsuarioParaCriar.Nome}/{UsuarioParaCriar.Cargo}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await UsuarioService.ReceivedWithAnyArgs(1).GetUsuario(Arg.Any<UsuarioDto>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetUsuarioWithInvalidModel()
        {
            var response = await TestClient.GetAsync($"/Usuario/GetUsuarioByNomeAndCargo/a/b");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await UsuarioService.ReceivedWithAnyArgs(0).GetUsuario(Arg.Any<UsuarioDto>());
        }
    }
}
