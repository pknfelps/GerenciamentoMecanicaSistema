using Domain.Costumer;
using Domain.Interface.Costumer;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto;

#pragma warning disable CA1859
namespace ServiceTests
{
    public class ClienteServiceTests
    {
        private IClienteService ClienteService { get; set; }
        private IClienteRepository ClienteRepository { get; set; }

        private static readonly ClienteDto ClienteToCreate = new("Fulano", "12345678912", "11912345678", "fulano@gmail.com");
        private static readonly ClienteDto ClienteToCreateFormated = new("Fulano", "123.456.789-12", "(11) 91234-5678", "fulano@gmail.com");
        private static readonly ClienteDto ClienteToFailCreation = new("Fulano Fail", "123.123.123-12", "(11) 99999-9999", "fail@gmail.com");
        private static readonly List<ICliente> ExistingClientes =
        [
            new Cliente("Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com"),
            new Cliente("Beltrano", "12.123.456/0001-15", "(11) 93214-6578", "beltrano@gmail.com"),
        ];
        private static readonly List<ClienteDto> ExistingClientesDtos =
        [
            new ("Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com"),
            new ("Beltrano", "12.123.456/0001-15", "(11) 93214-6578", "beltrano@gmail.com"),
        ];
        private static readonly ClienteDto ExistingClienteDto = new("Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com");
        private static readonly ClienteDto ClienteToUpdateDto = new("Ciclano", "12.123.456/0001-12", "(11) 94321-8765", "ciclano.company@gmail.com");
        private static readonly ClienteDto ClienteToFailtUpdateDto = new("Ciclano", "12.123.456/0001-15", "(11) 94321-8765", "ciclano.company@gmail.com");

        [SetUp]
        public void SetUp()
        {
            ClienteRepository = Substitute.For<IClienteRepository>();

            ClienteRepository.CreateCliente(Arg.Any<ICliente>()).Returns(callInfo =>
            {
                var cliente = callInfo.ArgAt<ICliente>(0);

                if (cliente.Documento.Id.Equals(ClienteToCreateFormated.Documento))
                    return 1;

                return 0;
            });

            ClienteRepository.GetClientes().Returns(ExistingClientes);

            ClienteRepository.GetClienteByDocumento(Arg.Any<string>()).Returns(callInfo =>
            {
                string documento = callInfo.ArgAt<string>(0);

                return ExistingClientes.FirstOrDefault(x => x.Documento.Id.Equals(documento));
            });

            ClienteRepository.CheckIfClienteExists(Arg.Any<string>()).Returns(callInfo =>
            {
                string documento = callInfo.ArgAt<string>(0);

                return ExistingClientes.FirstOrDefault(x => x.Documento.Id.Equals(documento)) != null;
            });

            ClienteRepository.UpdateCliente(Arg.Any<ICliente>()).Returns(callInfo =>
            {
                var cliente = callInfo.ArgAt<ICliente>(0);

                if (cliente.Documento.Id.Equals(ExistingClientes[0].Documento.Id))
                    return 1;

                return 0;
            });

            ClienteRepository.DeleteCliente(Arg.Any<string>()).Returns(callInfo =>
            {
                var documento = callInfo.ArgAt<string>(0);

                if (documento.Equals(ExistingClientes[0].Documento.Id))
                    return 1;

                return 0;
            });

            ClienteService = new ClienteService(ClienteRepository);
        }

        [Test]
        public async Task MustCreateCliente()
        {
            await ClienteService.CreateCliente(ClienteToCreate);

            await ClienteRepository.Received(1).CheckIfClienteExists(ClienteToCreateFormated.Documento);
            await ClienteRepository.ReceivedWithAnyArgs(1).CreateCliente(Arg.Any<ICliente>());
        }

        [Test]
        public async Task MustNotCreateClienteIfExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await ClienteService.CreateCliente(ExistingClienteDto));

            await ClienteRepository.Received(1).CheckIfClienteExists(ExistingClientes[0].Documento.Id);
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToCreateCliente()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await ClienteService.CreateCliente(ClienteToFailCreation));

            await ClienteRepository.Received(1).CheckIfClienteExists(ClienteToFailCreation.Documento);
            await ClienteRepository.ReceivedWithAnyArgs(1).CreateCliente(Arg.Any<ICliente>());
        }

        [Test]
        public async Task MustGetAllClientes()
        {
            var clientes = (await ClienteService.GetClientes()).ToList();

            await ClienteRepository.Received(1).GetClientes();

            Assert.That(clientes, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(clientes[0], Is.EqualTo(ExistingClientesDtos[0]));
                Assert.That(clientes[1], Is.EqualTo(ExistingClientesDtos[1]));
            });
        }

        [Test]
        public async Task MustGetClienteByDocumento()
        {
            var cliente = await ClienteService.GetClienteByDocumento(ExistingClientes[0].Documento.Id);

            await ClienteRepository.Received(1).GetClienteByDocumento(ExistingClientes[0].Documento.Id);

            Assert.That(cliente, Is.EqualTo(ExistingClientesDtos[0]));
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
            await ClienteService.UpdateCliente(ClienteToUpdateDto);

            await ClienteRepository.Received(1).CheckIfClienteExists(ClienteToUpdateDto.Documento);
            await ClienteRepository.Received(1).UpdateCliente(Arg.Any<ICliente>());
        }

        [Test]
        public async Task MustNotUpdateClienteIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await ClienteService.UpdateCliente(ClienteToCreate));

            await ClienteRepository.Received(1).CheckIfClienteExists(ClienteToCreateFormated.Documento);
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToUpdate()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await ClienteService.UpdateCliente(ClienteToFailtUpdateDto));

            await ClienteRepository.Received(1).CheckIfClienteExists(ClienteToFailtUpdateDto.Documento);
            await ClienteRepository.Received(1).UpdateCliente(Arg.Any<ICliente>());
        }

        [Test]
        public async Task MustDeleteCliente()
        {
            await ClienteService.DeleteCliente(ExistingClientes[0].Documento.Id);

            await ClienteRepository.Received(1).CheckIfClienteExists(ExistingClientes[0].Documento.Id);
            await ClienteRepository.Received(1).DeleteCliente(ExistingClientes[0].Documento.Id);
        }

        [Test]
        public async Task MustNotDeleteClienteIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await ClienteService.DeleteCliente(ClienteToCreate.Documento));

            await ClienteRepository.Received(1).CheckIfClienteExists(ClienteToCreateFormated.Documento);
        }

        [Test]
        public async Task MustThrowExceptionIfFailtToDelete()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await ClienteService.DeleteCliente(ExistingClientes[1].Documento.Id));

            await ClienteRepository.Received(1).CheckIfClienteExists(ExistingClientes[1].Documento.Id);
            await ClienteRepository.Received(1).DeleteCliente(ExistingClientes[1].Documento.Id);
        }
    }
}
