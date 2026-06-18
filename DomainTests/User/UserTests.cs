using Domain.Interface.User;

namespace DomainTests.User
{
    public class UserTests
    {
        private static readonly string NameUser = "Fulano";
        private static readonly string PasswordUser = "Senha@123";
        private static readonly string RoleUser = Roles.Manager.ToString();

        [Test]
        public void MustCreateUser()
        {
            var usuario = new Domain.User.User(NameUser, PasswordUser, RoleUser);

            Assert.That(usuario, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(usuario.Name, Is.EqualTo(NameUser));
                Assert.That(usuario.Password.Secret, Is.EqualTo(PasswordUser));
                Assert.That(usuario.Role.ToString(), Is.EqualTo(RoleUser));
            });
        }

        [Test]
        public void MustNotCreateUserIfNomeIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User("", PasswordUser, RoleUser));
            Assert.Throws<ArgumentException>(() => new Domain.User.User(" ", PasswordUser, RoleUser));
        }

        [Test]
        public void MustNotCreateUserIfSenhaIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NameUser, "", RoleUser));
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NameUser, " ", RoleUser));
        }

        [Test]
        public void MustNotCreateUserIfCargoIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NameUser, PasswordUser, ""));
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NameUser, PasswordUser, " "));
        }

        [Test]
        public void MustNotCreateUserIfSenhaIsEqualToNome()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NameUser, NameUser, RoleUser));
        }

        [Test]
        public void MustNotCreateUserIfSenhaIsEqualToCargo()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NameUser, RoleUser, RoleUser));
        }

        [Test]
        public void MustNotCreateUserIfCargoIsInvalid()
        {
            Assert.Throws<ArgumentException>(() => new Domain.User.User(NameUser, PasswordUser, "Customer"));
        }
    }
}
