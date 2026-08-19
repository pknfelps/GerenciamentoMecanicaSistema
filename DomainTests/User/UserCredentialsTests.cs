using Domain.Interface.User;
using Domain.User;

namespace DomainTests.User
{
    public class UserCredentialsTests
    {
        private const string PasswordHash = "pbkdf2-sha256$100000$stored-salt$stored-hash";
        private static IUser User { get; } = new Domain.User.User("Fulano", Roles.Manager.ToString());

        [Test]
        public void MustCreateUserCredentials()
        {
            var credentials = new UserCredentials(User, PasswordHash);

            Assert.Multiple(() =>
            {
                Assert.That(credentials.User.Name, Is.EqualTo(User.Name));
                Assert.That(credentials.User.Role, Is.EqualTo(Roles.Manager));
                Assert.That(credentials.PasswordHash, Is.EqualTo(PasswordHash));
            });
        }

        [Test]
        public void MustNotCreateCredentialsWithoutPasswordHash()
        {
            Assert.Throws<ArgumentException>(() => new UserCredentials(User, ""));
        }
    }
}
