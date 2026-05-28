using Domain;
using Domain.Interface;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto;

#pragma warning disable CA1859
namespace ServiceTests
{
    public class UsuarioServiceTests
    {
        private IUsuarioService UsuarioService { get; set; }
        private IUsuarioRepository UsuarioRepository { get; set; }

        private static readonly UsuarioDto UsuarioParaCadastrar = new("Fulano", "Senha@123", "Gerente");
        private static readonly UsuarioDto UsuarioParaFalhar = new("Teste", "Teste@123", "Usuario");
        private static readonly List<IUsuario> UsuariosExistentes =
        [
            new Usuario("Admin", "Admin@123", "Admin")
        ];
        private static readonly UsuarioDto UsuarioExistenteDto = new("Admin", "Admin@123", "Admin");

        [SetUp]
        public void SetUp()
        {
            UsuarioRepository = Substitute.For<IUsuarioRepository>();

            UsuarioRepository.RegisterUsuario(Arg.Any<IUsuario>()).Returns(callInfo =>
            {
                var usuario = callInfo.ArgAt<IUsuario>(0);

                if (usuario.Nome == UsuarioParaCadastrar.Nome && usuario.Senha.Senha == UsuarioParaCadastrar.Senha && usuario.Cargo.ToString() == UsuarioParaCadastrar.Cargo)
                    return 1;

                if (usuario.Nome == UsuarioParaFalhar.Nome && usuario.Senha.Senha == UsuarioParaFalhar.Senha && usuario.Cargo.ToString() == UsuarioParaFalhar.Cargo)
                    throw new InvalidOperationException();

                return 0;
            });

            UsuarioRepository.CheckIfUsuarioExists(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var nome = callInfo.ArgAt<string>(0);
                var cargo = callInfo.ArgAt<string>(1);

                if (nome == UsuariosExistentes[0].Nome && cargo == UsuariosExistentes[0].Cargo.ToString())
                    return true;

                return false;
            });

            UsuarioRepository.GetUsuarioByNomeAndCargo(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var nome = callInfo.ArgAt<string>(0);
                var cargo = callInfo.ArgAt<string>(1);

                return UsuariosExistentes.FirstOrDefault(x => x.Nome == nome && x.Cargo.ToString() == cargo);
            });

            UsuarioService = new UsuarioService(UsuarioRepository);
        }

        [Test]
        public async Task MustCreateUsuario()
        {
            await UsuarioService.RegisterUsuario(UsuarioParaCadastrar);

            await UsuarioRepository.ReceivedWithAnyArgs(1).CheckIfUsuarioExists(Arg.Any<string>(), Arg.Any<string>());
            await UsuarioRepository.ReceivedWithAnyArgs(1).RegisterUsuario(Arg.Any<IUsuario>());
        }

        [Test]
        public async Task MustNotCreateUsuarioIfExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await UsuarioService.RegisterUsuario(UsuarioExistenteDto));

            await UsuarioRepository.ReceivedWithAnyArgs(1).CheckIfUsuarioExists(Arg.Any<string>(), Arg.Any<string>());
            await UsuarioRepository.ReceivedWithAnyArgs(0).RegisterUsuario(Arg.Any<IUsuario>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailRegister()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await UsuarioService.RegisterUsuario(UsuarioParaFalhar));

            await UsuarioRepository.ReceivedWithAnyArgs(1).CheckIfUsuarioExists(Arg.Any<string>(), Arg.Any<string>());
            await UsuarioRepository.ReceivedWithAnyArgs(1).RegisterUsuario(Arg.Any<IUsuario>());
        }

        [Test]
        public async Task MustGetUsuario()
        {
            var usuario = await UsuarioService.GetUsuario(UsuarioExistenteDto);

            await UsuarioRepository.ReceivedWithAnyArgs(1).GetUsuarioByNomeAndCargo(Arg.Any<string>(), Arg.Any<string>());

            Assert.That(usuario, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(usuario.Nome, Is.EqualTo(UsuarioExistenteDto.Nome));
                Assert.That(usuario.Cargo, Is.EqualTo(UsuarioExistenteDto.Cargo));
            });
        }

        [Test]
        public async Task MustNotGetUsuarioIfNotExists()
        {
            var usuario = await UsuarioService.GetUsuario(UsuarioParaCadastrar);

            await UsuarioRepository.ReceivedWithAnyArgs(1).GetUsuarioByNomeAndCargo(Arg.Any<string>(), Arg.Any<string>());

            Assert.That(usuario, Is.Null);
        }
    }
}
