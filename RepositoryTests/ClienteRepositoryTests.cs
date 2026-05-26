using Dapper;
using DTOs;
using Npgsql;
using Repository;
using Repository.Interface;
using System.Data;
using Testcontainers.PostgreSql;

namespace RepositoryTests
{
    public class ClienteRepositoryTests
    {
        private PostgreSqlContainer PostgresContainer { get; set; }
        private IDbConnection Connection { get; set; }
        private IClienteRepository Repository { get; set; }

        private readonly ClienteDto ClienteToCreate = new(Guid.NewGuid(), "Fulano", "123.456.789-12", "(11) 31234-5678", "fulano@gmail.com");

        private static readonly Guid ExistingClienteId = Guid.NewGuid();

        private readonly List<ClienteDto> ExistingClientes =
        [
            new ClienteDto(ExistingClienteId, "Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com"),
            new ClienteDto(Guid.NewGuid(), "Beltrano", "12.123.456/0001-15", "(11) 93214-6578", "beltrano@gmail.com"),
        ];

        private readonly ClienteDto ClienteToUpdate = new(ExistingClienteId, "Ciclano", "12.123.456/0001-12", "(11) 94321-8765", "ciclano.company@gmail.com");

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
                CREATE TABLE IF NOT EXISTS public.clientes (
                id UUID PRIMARY KEY,
                nome TEXT,
                documento TEXT,
                celular TEXT,
                email TEXT);
                """);

            Repository = new ClienteRepository(Connection);

            foreach (ClienteDto cliente in ExistingClientes)
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
            await Repository.CreateCliente(ClienteToCreate);

            var cliente = await Repository.GetClienteByDocumento(ClienteToCreate.Documento);

            Assert.That(cliente, Is.Not.Null);
            Assert.That(cliente.Equals(ClienteToCreate), Is.True);
        }

        [Test]
        public async Task MustGetAllClientes()
        {
            var clientes = (await Repository.GetClientes()).ToList();

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
            var cliente = await Repository.GetClienteByDocumento(ExistingClientes[0].Documento);

            Assert.That(cliente, Is.EqualTo(ExistingClientes[0]));
        }

        [Test]
        public async Task MustGetClienteByDocumentoWithWrongDocumento()
        {
            ClienteDto? cliente = await Repository.GetClienteByDocumento(ClienteToCreate.Documento);

            Assert.That(cliente, Is.Null);
        }

        [Test]
        public async Task MustUpdateCliente()
        {
            await Repository.UpdateCliente(ClienteToUpdate);

            var cliente = await Repository.GetClienteByDocumento(ExistingClientes[0].Documento);

            Assert.That(cliente, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Equals(ExistingClientes[0]), Is.False);
                Assert.That(cliente.Equals(ClienteToUpdate), Is.True);
            });
        }

        [Test]
        public async Task MustDeleteCliente()
        {
            await Repository.DeleteCliente(ExistingClientes[0].Documento);

            ClienteDto? cliente = await Repository.GetClienteByDocumento(ExistingClientes[0].Documento);

            Assert.That(cliente, Is.Null);
        }

        [Test]
        public async Task MustReturnTrueWhenCheckIfClienteExistsWithExistingCliente()
        {
            var result = await Repository.CheckIfClienteExists(ExistingClientes[0].Documento);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task MustReturnFalseWhenCheckIfClienteExistsWithNotAnExistingCliente()
        {
            var result = await Repository.CheckIfClienteExists(ClienteToCreate.Documento);

            Assert.That(result, Is.False);
        }
    }
}