using Domain.Interface.User;
using Domain.User;

namespace DomainTests.User
{
    public class UsuarioTests
    {
        private static readonly string NomeUsuario = "Fulano";
        private static readonly string SenhaUsuario = "Senha@123";
        private static readonly string CargoUsuario = Roles.Manager.ToString();

        [Test]
        public void MustCreateUsuario()
        {
            var usuario = new Domain.User.User(NomeUsuario, SenhaUsuario, CargoUsuario);

            Assert.That(usuario, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(usuario.Name, Is.EqualTo(NomeUsuario));
                Assert.That(usuario.Password.Secret, Is.EqualTo(SenhaUsuario));
                Assert.That(usuario.Role.ToString(), Is.EqualTo(CargoUsuario));
            });
        }

        [Test]
        public void MustNotCreateUsuarioIfNomeIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User("", SenhaUsuario, CargoUsuario));
            Assert.Throws<ArgumentException>(() => new Domain.User.User(" ", SenhaUsuario, CargoUsuario));
        }

        [Test]
        public void MustNotCreateUsuarioIfSenhaIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NomeUsuario, "", CargoUsuario));
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NomeUsuario, " ", CargoUsuario));
        }

        [Test]
        public void MustNotCreateUsuarioIfCargoIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NomeUsuario, SenhaUsuario, ""));
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NomeUsuario, SenhaUsuario, " "));
        }

        [Test]
        public void MustNotCreateUsuarioIfSenhaIsEqualToNome()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NomeUsuario, NomeUsuario, CargoUsuario));
        }

        [Test]
        public void MustNotCreateUsuarioIfSenhaIsEqualToCargo()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NomeUsuario, CargoUsuario, CargoUsuario));
        }

        [Test]
        public void MustNotCreateUsuarioIfCargoIsInvalid()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NomeUsuario, SenhaUsuario, "Cliente"));
        }
    }
}
