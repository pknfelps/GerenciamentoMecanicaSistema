using DTOs;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Service.Interface;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace ControllerTests
{
    public partial class Tests
    {
        private TestWebApplicationFactory TestWebAppFactory { get; set; }
        private HttpClient TestClient {get;set;}
        private IClienteService ClienteService {get;set;}

        private readonly ClienteDto ClienteToCreate = new(Guid.NewGuid(), "Fulano", "12345678912", "11912345678", "fulano@gmail.com");

        private static readonly Guid ExistingClienteGuid = Guid.NewGuid();

        private readonly List<ClienteDto> ExistingClientes =
        [
            new ClienteDto(ExistingClienteGuid, "Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com"),
            new ClienteDto(Guid.NewGuid(), "Beltrano", "12.123.456/0001-15", "(11) 93214-6578", "beltrano@gmail.com"),
        ];

        private readonly ClienteDto ClienteToUpdate = new(ExistingClienteGuid, "Ciclano", "12.123.456/0001-12", "(11) 94321-8765", "ciclano.company@gmail.com");

        [SetUp]
        public void Setup()
        {
            TestWebAppFactory = new TestWebApplicationFactory();
            TestClient = TestWebAppFactory.CreateClient();
            ClienteService = TestWebAppFactory.ClienteServiceMock;
        }

        [TearDown]
        public void TearDown()
        {
            TestClient.Dispose();
            TestWebAppFactory.Dispose();
        }

        [Test]
        public async Task MustCreateCliente()
        {
            ClienteService.CreateCliente(ClienteToCreate).Returns(Task.CompletedTask);

            var response = await TestClient.PostAsJsonAsync("/Cliente/CreateCliente", ClienteToCreate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await ClienteService.Received(1).CreateCliente(ClienteToCreate);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryCreateAClienteThatIsNotValid()
        {
            var response = await TestClient.PostAsJsonAsync("/Cliente/CreateCliente", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await ClienteService.ReceivedWithAnyArgs(0).CreateCliente(Arg.Any<ClienteDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryCreateAClienteThatAlreadyExists()
        {
            ClienteService.CreateCliente(Arg.Any<ClienteDto>()).Throws<InvalidOperationException>();

            var response = await TestClient.PostAsJsonAsync("/Cliente/CreateCliente", ExistingClientes[0]);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await ClienteService.Received(1).CreateCliente(ExistingClientes[0]);
        }

        [Test]
        public async Task MustGetClientes()
        {
            ClienteService.GetClientes().Returns(ExistingClientes);

            var response = await TestClient.GetAsync("/Cliente/GetClientes");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var resultado = await response.Content.ReadFromJsonAsync<IEnumerable<ClienteDto>>();
            var clientes = resultado?.ToList();

            await ClienteService.Received(1).GetClientes();

            Assert.That(clientes, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(clientes[0].Equals(ExistingClientes[0]), Is.True);
                Assert.That(clientes[1].Equals(ExistingClientes[1]), Is.True);
            });
        }

        [Test]
        public async Task MustGetClienteByDocumento()
        {
            ClienteService.GetClienteByDocumento(Arg.Any<string>()).Returns(callInfo =>
            {
                var documento = callInfo.ArgAt<string>(0);

                return ExistingClientes.FirstOrDefault(x => RegexRemovePunctuation().Replace(x.Documento, "") == documento);
            });

            var documento = RegexRemovePunctuation().Replace(ExistingClientes[0].Documento, "");

            var response = await TestClient.GetAsync($"/Cliente/GetClienteByDocumento/{documento}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var cliente = await response.Content.ReadFromJsonAsync<ClienteDto>();

            await ClienteService.Received(1).GetClienteByDocumento(documento);

            Assert.That(cliente, Is.Not.Null);
            Assert.That(cliente.Equals(ExistingClientes[0]), Is.True);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryGetClienteClienteThatNotExists()
        {
            ClienteService.GetClienteByDocumento(Arg.Any<string>()).Returns((ClienteDto?)null);

            var response = await TestClient.GetAsync($"/Cliente/GetClienteByDocumento/{ClienteToCreate.Documento}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await ClienteService.Received(1).GetClienteByDocumento(ClienteToCreate.Documento);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetClienteWithInvalidDocumento()
        {
            var response = await TestClient.GetAsync($"/Cliente/GetClienteByDocumento/teste");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await ClienteService.ReceivedWithAnyArgs(0).GetClienteByDocumento(Arg.Any<string>());
        }

        [Test]
        public async Task MustUpdateCliente()
        {
            ClienteService.UpdateCliente(ClienteToUpdate).Returns(Task.CompletedTask);

            var response = await TestClient.PatchAsJsonAsync("/Cliente/UpdateCliente", ClienteToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await ClienteService.Received(1).UpdateCliente(ClienteToUpdate);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateWithInvalidClient()
        {
            var response = await TestClient.PatchAsJsonAsync("/Cliente/UpdateCliente", new { Nome = "Teste" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await ClienteService.ReceivedWithAnyArgs(0).UpdateCliente(Arg.Any<ClienteDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryUpdateAClienteThatNotExists()
        {
            ClienteService.UpdateCliente(ClienteToCreate).Throws<InvalidOperationException>();

            var response = await TestClient.PatchAsJsonAsync("/Cliente/UpdateCliente", ClienteToCreate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await ClienteService.Received(1).UpdateCliente(ClienteToCreate);
        }

        [Test]
        public async Task MustDeleteCliente()
        {
            var documento = RegexRemovePunctuation().Replace(ExistingClientes[0].Documento, "");

            ClienteService.DeleteCliente(documento).Returns(Task.CompletedTask);

            var response = await TestClient.DeleteAsync($"/Cliente/DeleteCliente/{documento}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await ClienteService.Received(1).DeleteCliente(documento);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteAClienteWithInvalidDocumento()
        {
            var response = await TestClient.DeleteAsync($"/Cliente/DeleteCliente/teste");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await ClienteService.ReceivedWithAnyArgs(0).DeleteCliente(Arg.Any<string>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryDeleteAClienteThatNotExists()
        {
            ClienteService.DeleteCliente(ClienteToCreate.Documento).Throws<InvalidOperationException>();

            var response = await TestClient.DeleteAsync($"/Cliente/DeleteCliente/{ClienteToCreate.Documento}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await ClienteService.Received(1).DeleteCliente(ClienteToCreate.Documento);
        }

        [GeneratedRegex(@"\p{P}")]
        private static partial Regex RegexRemovePunctuation();
    }
}
