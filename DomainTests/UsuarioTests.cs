using Domain;
using Domain.Interface;

namespace DomainTests
{
    public class UsuarioTests
    {
        private static readonly string NomeUsuario = "Fulano";
        private static readonly string SenhaUsuario = "Senha@123";
        private static readonly string CargoUsuario = Cargos.Gerente.ToString();

        [Test]
        public void MustCreateUsuario()
        {
            var usuario = new Usuario(NomeUsuario, SenhaUsuario, CargoUsuario);

            Assert.That(usuario, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(usuario.Nome, Is.EqualTo(NomeUsuario));
                Assert.That(usuario.Senha.Senha, Is.EqualTo(SenhaUsuario));
                Assert.That(usuario.Cargo.ToString(), Is.EqualTo(CargoUsuario));
            });
        }

        [Test]
        public void MustNotCreateUsuarioIfNomeIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Usuario("", SenhaUsuario, CargoUsuario));
            Assert.Throws<ArgumentException>(() => new Usuario(" ", SenhaUsuario, CargoUsuario));
        }

        [Test]
        public void MustNotCreateUsuarioIfSenhaIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Usuario(NomeUsuario, "", CargoUsuario));
            Assert.Throws<ArgumentException>(() => new Usuario(NomeUsuario, " ", CargoUsuario));
        }

        [Test]
        public void MustNotCreateUsuarioIfCargoIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Usuario(NomeUsuario, SenhaUsuario, ""));
            Assert.Throws<ArgumentException>(() => new Usuario(NomeUsuario, SenhaUsuario, " "));
        }

        [Test]
        public void MustNotCreateUsuarioIfSenhaIsEqualToNome()
        {
            Assert.Throws<ArgumentException>(() => new Usuario(NomeUsuario, NomeUsuario, CargoUsuario));
        }

        [Test]
        public void MustNotCreateUsuarioIfSenhaIsEqualToCargo()
        {
            Assert.Throws<ArgumentException>(() => new Usuario(NomeUsuario, CargoUsuario, CargoUsuario));
        }

        [Test]
        public void MustNotCreateUsuarioIfCargoIsInvalid()
        {
            Assert.Throws<ArgumentException>(() => new Usuario(NomeUsuario, SenhaUsuario, "Cliente"));
        }
    }
}
