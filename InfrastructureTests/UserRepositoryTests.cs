using Dapper;
using Domain.Interface.User;
using Domain.User;
using Infrastructure.Persistence.PostgreSql.Repositories;
using Infrastructure.Interface.Persistence;

namespace InfrastructureTests
{
    public class UserRepositoryTests : BaseRepositoryTests
    {
        private IUserRepository Repository { get; set; }

        private static IUserCredentials UserToRegister { get; } = new UserCredentials(
            new User(Guid.NewGuid(), "Fulano", Roles.Manager.ToString()),
            "Stored-password-hash1!");

        private static IUserCredentials ExistingUser { get; } = new UserCredentials(
            new User(Guid.NewGuid(), "Admin", Roles.Admin.ToString()),
            "Stored-admin-password-hash1!");

        protected override async Task InternalSetup()
        {
            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                password VARCHAR(100) NOT NULL,
                role VARCHAR(100) NOT NULL);
                """);

            Repository = new UserRepository(Connection);

            await Repository.RegisterUser(ExistingUser);
        }

        [Test]
        public async Task MustRegisterUser()
        {
            var registro = await Repository.RegisterUser(UserToRegister);

            Assert.That(registro, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustGetUserByNomeAndCargo()
        {
            var User = await Repository.GetUser(ExistingUser.User.Name, ExistingUser.User.Role.ToString());

            Assert.That(User, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(User.Name, Is.EqualTo(ExistingUser.User.Name));
                Assert.That(User.Role, Is.EqualTo(ExistingUser.User.Role));
            });
        }

        [Test]
        public async Task MustGetUserCredentialsByNameAndRole()
        {
            var credentials = await Repository.GetUserCredentials(
                ExistingUser.User.Name,
                ExistingUser.User.Role.ToString());

            Assert.That(credentials, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(credentials.User.Id, Is.EqualTo(ExistingUser.User.Id));
                Assert.That(credentials.PasswordHash, Is.EqualTo(ExistingUser.PasswordHash));
            });
        }

        [Test]
        public async Task MustNotGetUserByNomeAndCargoIfNotExists()
        {
            var User = await Repository.GetUser(UserToRegister.User.Name, UserToRegister.User.Role.ToString());

            Assert.That(User, Is.Null);
        }
    }
}
