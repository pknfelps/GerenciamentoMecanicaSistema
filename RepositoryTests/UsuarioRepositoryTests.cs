using Dapper;
using Domain.Interface.User;
using Domain.User;
using Repository;
using Repository.Interface;

#pragma warning disable CA1859
namespace RepositoryTests
{
    public class UsuarioRepositoryTests : BaseRepositoryTests
    {
        private IUsuarioRepository Repository { get; set; }

        private static readonly IUsuario UsuarioParaCadastrar = new Usuario("Fulano", "Senha@123", "Gerente");
        private static readonly List<IUsuario> UsuariosExistentes =
        [
            new Usuario("Admin", "Admin@123", "Admin")
        ];

        protected override async Task InternalSetup()
        {
            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS usuarios (
                id UUID PRIMARY KEY NOT NULL,
                nome TEXT NOT NULL,
                senha TEXT NOT NULL,
                cargo TEXT NOT NULL);
                """);

            Repository = new UsuarioRepository(Connection);

            foreach (IUsuario usuario in UsuariosExistentes)
                await Repository.RegisterUsuario(usuario);
        }

        [Test]
        public async Task MustRegisterUsuario()
        {
            var registro = await Repository.RegisterUsuario(UsuarioParaCadastrar);

            Assert.That(registro, Is.Not.EqualTo(0));
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
