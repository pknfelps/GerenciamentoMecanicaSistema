using Domain.Interface.Exceptions;
using Domain.Interface.User;

namespace DomainTests.User
{
    public class UserTests
    {
        private static readonly string NameUser = "Fulano";
        private static readonly string RoleUser = Roles.Manager.ToString();

        [Test]
        public void MustCreateUser()
        {
            var usuario = new Domain.User.User(NameUser, RoleUser);

            Assert.That(usuario, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(usuario.Name, Is.EqualTo(NameUser));
                Assert.That(usuario.Role.ToString(), Is.EqualTo(RoleUser));
            });
        }

        [Test]
        public void MustNotCreateUserIfNomeIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.User.User("", RoleUser));
            Assert.Catch<DomainValidationException>(() => new Domain.User.User(" ", RoleUser));
        }

        [Test]
        public void MustNotCreateUserIfCargoIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.User.User(NameUser, ""));
            Assert.Catch<DomainValidationException>(() => new Domain.User.User(NameUser, " "));
        }

        [Test]
        public void MustNotCreateUserIfCargoIsInvalid()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.User.User(NameUser, "Customer"));
        }
    }
}

