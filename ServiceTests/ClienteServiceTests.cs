using DTOs;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Interface;
using Service;
using Service.Interface;

namespace ServiceTests
{
    public class ClienteServiceTests
    {
        private IClienteService ClienteService { get; set; }
        private IClienteRepository ClienteRepository { get; set; }

        private readonly ClienteDto ClienteToCreate = new(Guid.Empty, "Fulano", "12345678912", "11912345678", "fulano@gmail.com");
        private readonly ClienteDto ClienteToCreateFormated = new(Guid.Empty, "Fulano", "123.456.789-12", "(11) 91234-5678", "fulano@gmail.com");

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
            ClienteRepository = Substitute.For<IClienteRepository>();

            ClienteRepository.CreateCliente(Arg.Any<ClienteDto>()).Returns(Task.CompletedTask);
            ClienteRepository.GetClientes().Returns(ExistingClientes);
            ClienteRepository.GetClienteByDocumento(Arg.Any<string>()).Returns(callInfo =>
            {
                string documento = callInfo.ArgAt<string>(0);

                return ExistingClientes.FirstOrDefault(x => x.Documento.Equals(documento));
            });

            ClienteRepository.CheckIfClienteExists(Arg.Any<string>()).Returns(callInfo =>
            {
                string documento = callInfo.ArgAt<string>(0);

                return ExistingClientes.FirstOrDefault(x => x.Documento.Equals(documento)) != null;
            });

            ClienteRepository.UpdateCliente(Arg.Any<ClienteDto>()).Returns(Task.CompletedTask);
            ClienteRepository.DeleteCliente(ExistingClientes[0].Documento).Returns(Task.CompletedTask);
            ClienteRepository.DeleteCliente(ExistingClientes[1].Documento).Returns(Task.CompletedTask);
            ClienteRepository.DeleteCliente(ClienteToCreateFormated.Documento).Throws<InvalidOperationException>();

            ClienteService = new ClienteService(ClienteRepository);
        }

        [Test]
        public async Task MustCreateCliente()
        {
            await ClienteService.CreateCliente(ClienteToCreate);

            await ClienteRepository.Received(1).CheckIfClienteExists(ClienteToCreateFormated.Documento);
            await ClienteRepository.ReceivedWithAnyArgs(1).CreateCliente(Arg.Any<ClienteDto>());
        }

        [Test]
        public async Task MustNotCreateClienteIfExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await ClienteService.CreateCliente(ExistingClientes[0]));

            await ClienteRepository.Received(1).CheckIfClienteExists(ExistingClientes[0].Documento);
        }

        [Test]
        public async Task MustGetAllClientes()
        {
            var clientes = (await ClienteService.GetClientes()).ToList();
            
            await ClienteRepository.Received(1).GetClientes();

            Assert.That(clientes, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(clientes[0], Is.EqualTo(ExistingClientes[0]));
                Assert.That(clientes[1], Is.EqualTo(ExistingClientes[1]));
            });
        }

        [Test]
        public async Task MustGetClienteByDocumento()
        {
            var cliente = await ClienteService.GetClienteByDocumento(ExistingClientes[0].Documento);

            await ClienteRepository.Received(1).GetClienteByDocumento(ExistingClientes[0].Documento);

            Assert.That(cliente, Is.EqualTo(ExistingClientes[0]));
        }

        [Test]
        public async Task MustGetClienteByDocumentoWithWrongDocumento()
        {
            var cliente = await ClienteService.GetClienteByDocumento(ClienteToCreate.Documento);

            await ClienteRepository.Received(1).GetClienteByDocumento(ClienteToCreateFormated.Documento);

            Assert.That(cliente, Is.Null);
        }

        [Test]
        public async Task MustUpdateCliente()
        {
            await ClienteService.UpdateCliente(ClienteToUpdate);

            await ClienteRepository.Received(1).CheckIfClienteExists(ClienteToUpdate.Documento);
            await ClienteRepository.Received(1).UpdateCliente(ClienteToUpdate);
        }

        [Test]
        public async Task MustNotUpdateClienteIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await ClienteService.UpdateCliente(ClienteToCreate));

            await ClienteRepository.Received(1).CheckIfClienteExists(ClienteToCreateFormated.Documento);
        }

        [Test]
        public async Task MustDeleteCliente()
        {
            await ClienteService.DeleteCliente(ExistingClientes[0].Documento);

            await ClienteRepository.Received(1).CheckIfClienteExists(ExistingClientes[0].Documento);
            await ClienteRepository.Received(1).DeleteCliente(ExistingClientes[0].Documento);
        }

        [Test]
        public async Task MustNotDeleteClienteIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await ClienteService.DeleteCliente(ClienteToCreate.Documento));

            await ClienteRepository.Received(1).CheckIfClienteExists(ClienteToCreateFormated.Documento);
        }
    }
}
