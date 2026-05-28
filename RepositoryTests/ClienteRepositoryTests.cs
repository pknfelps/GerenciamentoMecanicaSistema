using Dapper;
using Domain.Costumer;
using Domain.Interface.Costumer;
using Npgsql;
using Repository;
using Repository.Interface;
using System.Data;
using Testcontainers.PostgreSql;

#pragma warning disable CA1859
namespace RepositoryTests
{
    public class ClienteRepositoryTests
    {
        private PostgreSqlContainer PostgresContainer { get; set; }
        private IDbConnection Connection { get; set; }
        private IClienteRepository Repository { get; set; }

        private static readonly ICliente ClienteToCreate = new Cliente("Fulano", "123.456.789-12", "(11) 31234-5678", "fulano@gmail.com");
        private static readonly Guid ExistingClienteId = Guid.NewGuid();
        private static readonly List<ICliente> ExistingClientes =
        [
            new Cliente(ExistingClienteId, "Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com"),
            new Cliente("Beltrano", "12.123.456/0001-15", "(11) 93214-6578", "beltrano@gmail.com"),
        ];
        private static readonly ICliente ClienteToUpdate = new Cliente(ExistingClienteId, "Ciclano", "12.123.456/0001-12", "(11) 94321-8765", "ciclano.company@gmail.com");

        [SetUp]
        public async Task Setup()
        {
            PostgresContainer = new PostgreSqlBuilder("postgres:18")
                .WithDatabase("postgres")
                .WithUsername("postgres")
                .WithPassword("adm123")
                .Build();

            await PostgresContainer.StartAsync();

            Connection = new NpgsqlConnection(PostgresContainer.GetConnectionString());
            Connection.Open();

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS clientes (
                id UUID PRIMARY KEY,
                nome TEXT,
                documento TEXT,
                celular TEXT,
                email TEXT);
                """);

            Repository = new ClienteRepository(Connection);

            foreach (ICliente cliente in ExistingClientes)
                await Repository.CreateCliente(cliente);
        }

        [TearDown]
        public async Task TearDown()
        {
            await PostgresContainer.DisposeAsync();
            Connection.Dispose();
        }

        [Test]
        public async Task MustCreateCliente()
        {
            var registro = await Repository.CreateCliente(ClienteToCreate);

            Assert.That(registro, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustGetAllClientes()
        {
            var clientes = (await Repository.GetClientes()).ToList();

            Assert.That(clientes, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(clientes[0].Nome, Is.EqualTo(ExistingClientes[0].Nome));
                Assert.That(clientes[0].Documento.Id, Is.EqualTo(ExistingClientes[0].Documento.Id));
                Assert.That(clientes[0].Celular.Numero, Is.EqualTo(ExistingClientes[0].Celular.Numero));
            });

            Assert.Multiple(() =>
            {
                Assert.That(clientes[1].Nome, Is.EqualTo(ExistingClientes[1].Nome));
                Assert.That(clientes[1].Documento.Id, Is.EqualTo(ExistingClientes[1].Documento.Id));
                Assert.That(clientes[1].Celular.Numero, Is.EqualTo(ExistingClientes[1].Celular.Numero));
            });
        }

        [Test]
        public async Task MustGetClienteByDocumento()
        {
            var cliente = await Repository.GetClienteByDocumento(ExistingClientes[0].Documento.Id);

            Assert.That(cliente, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Nome, Is.EqualTo(ExistingClientes[0].Nome));
                Assert.That(cliente.Documento.Id, Is.EqualTo(ExistingClientes[0].Documento.Id));
                Assert.That(cliente.Celular.Numero, Is.EqualTo(ExistingClientes[0].Celular.Numero));
            });
        }

        [Test]
        public async Task MustGetClienteByDocumentoWithWrongDocumento()
        {
            ICliente? cliente = await Repository.GetClienteByDocumento(ClienteToCreate.Documento.Id);

            Assert.That(cliente, Is.Null);
        }

        [Test]
        public async Task MustUpdateCliente()
        {
            await Repository.UpdateCliente(ClienteToUpdate);

            var cliente = await Repository.GetClienteByDocumento(ExistingClientes[0].Documento.Id);

            Assert.That(cliente, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Id, Is.EqualTo(ExistingClientes[0].Id));
                Assert.That(cliente.Nome, Is.EqualTo(ExistingClientes[0].Nome));
                Assert.That(cliente.Documento.Id, Is.EqualTo(ExistingClientes[0].Documento.Id));
                Assert.That(cliente.Celular.Numero, Is.Not.EqualTo(ExistingClientes[0].Celular.Numero));
                Assert.That(cliente.Email.Endereco, Is.Not.EqualTo(ExistingClientes[0].Email.Endereco));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Id, Is.EqualTo(ClienteToUpdate.Id));
                Assert.That(cliente.Nome, Is.EqualTo(ClienteToUpdate.Nome));
                Assert.That(cliente.Documento.Id, Is.EqualTo(ClienteToUpdate.Documento.Id));
                Assert.That(cliente.Celular.Numero, Is.EqualTo(ClienteToUpdate.Celular.Numero));
                Assert.That(cliente.Email.Endereco, Is.EqualTo(ClienteToUpdate.Email.Endereco));
            });
        }

        [Test]
        public async Task MustDeleteCliente()
        {
            await Repository.DeleteCliente(ExistingClientes[0].Documento.Id);

            ICliente? cliente = await Repository.GetClienteByDocumento(ExistingClientes[0].Documento.Id);

            Assert.That(cliente, Is.Null);
        }

        [Test]
        public async Task MustReturnTrueWhenCheckIfClienteExistsWithExistingCliente()
        {
            var result = await Repository.CheckIfClienteExists(ExistingClientes[0].Documento.Id);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task MustReturnFalseWhenCheckIfClienteExistsWithNotAnExistingCliente()
        {
            var result = await Repository.CheckIfClienteExists(ClienteToCreate.Documento.Id);

            Assert.That(result, Is.False);
        }
    }
}