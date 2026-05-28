using Dapper;
using Domain;
using Domain.Interface;
using Npgsql;
using Repository;
using Repository.Interface;
using System.Data;
using Testcontainers.PostgreSql;

#pragma warning disable CA1859
namespace RepositoryTests
{
    public class UsuarioRepositoryTests
    {
        private PostgreSqlContainer PostgresContainer { get; set; }
        private IDbConnection Connection { get; set; }
        private IUsuarioRepository Repository { get; set; }

        private static readonly IUsuario UsuarioParaCadastrar = new Usuario("Fulano", "Senha@123", "Gerente");
        private static readonly List<IUsuario> UsuariosExistentes =
        [
            new Usuario("Admin", "Admin@123", "Admin")
        ];

        [SetUp]
        public async Task SetUp()
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
                CREATE TABLE IF NOT EXISTS usuarios (
                id UUID PRIMARY KEY,
                nome TEXT,
                senha TEXT,
                cargo TEXT);
                """);

            Repository = new UsuarioRepository(Connection);

            foreach (IUsuario usuario in UsuariosExistentes)
                await Repository.RegisterUsuario(usuario);
        }

        [TearDown]
        public async Task TearDown()
        {
            await PostgresContainer.DisposeAsync();
            Connection.Dispose();
        }

        [Test]
        public async Task MustRegisterUsuario()
        {
            var registro = await Repository.RegisterUsuario(UsuarioParaCadastrar);

            Assert.That(registro, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustReturnTrueWhenCheckIfUsuarioExistsWithExistingUsuario()
        {
            var result = await Repository.CheckIfUsuarioExists(UsuariosExistentes[0].Nome, UsuariosExistentes[0].Cargo.ToString());

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task MustReturnFalseWhenCheckIfUsuarioExistsWithNotExistingUsuario()
        {
            var result = await Repository.CheckIfUsuarioExists(UsuarioParaCadastrar.Nome, UsuarioParaCadastrar.Cargo.ToString());

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task MustGetUsuarioByNomeAndCargo()
        {
            var usuario = await Repository.GetUsuarioByNomeAndCargo(UsuariosExistentes[0].Nome, UsuariosExistentes[0].Cargo.ToString());

            Assert.That(usuario, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(usuario.Nome, Is.EqualTo(UsuariosExistentes[0].Nome));
                Assert.That(usuario.Cargo, Is.EqualTo(UsuariosExistentes[0].Cargo));
            });
        }

        [Test]
        public async Task MustNotGetUsuarioByNomeAndCargoIfNotExists()
        {
            var usuario = await Repository.GetUsuarioByNomeAndCargo(UsuarioParaCadastrar.Nome, UsuarioParaCadastrar.Cargo.ToString());

            Assert.That(usuario, Is.Null);
        }
    }
}
