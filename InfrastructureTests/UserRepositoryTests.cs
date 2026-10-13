using Dapper;
using Domain.Interface.User;
using Domain.User;
using Infrastructure.Persistence.PostgreSql.Repositories;
using Service.Interface.Persistence;
using Domain.Interface.Exceptions;

namespace InfrastructureTests
{
    public class UserRepositoryTests : BaseRepositoryTests
    {
        [Test]
        public async Task DeleteRemovesOnlyTargetAndRepeatedDeleteAffectsNoRows()
        {
            await Repository.RegisterUser(UserToRegister);
            Assert.That(await Repository.DeleteUser(ExistingUser.User.Id), Is.EqualTo(1));
            Assert.That(await Repository.GetUserById(ExistingUser.User.Id), Is.Null);
            Assert.That((await Repository.GetUserById(UserToRegister.User.Id))!.Name, Is.EqualTo(UserToRegister.User.Name));
            Assert.That(await Repository.DeleteUser(ExistingUser.User.Id), Is.Zero);
            Assert.That(await Repository.DeleteUser(Guid.NewGuid()), Is.Zero);
        }

        [TestCase("Changed name", null)]
        [TestCase(null, "Changed-password-hash")]
        [TestCase("Changed name", "Changed-password-hash")]
        public async Task UpdatePreservesIdentityRoleAndOmittedFields(string? name, string? hash)
        {
            Assert.That(await Repository.UpdateUser(ExistingUser.User.Id, name, hash), Is.EqualTo(1));
            var credentials = await Repository.GetUserCredentials(name ?? ExistingUser.User.Name, "Admin");
            Assert.That(credentials!.User.Id, Is.EqualTo(ExistingUser.User.Id));
            Assert.That(credentials.User.Role, Is.EqualTo(Roles.Admin));
            Assert.That(credentials.PasswordHash, Is.EqualTo(hash ?? ExistingUser.PasswordHash));
        }

        [Test]
        public async Task UpdateTargetsOnlyItsUuidAndUsesParameters()
        {
            await Repository.RegisterUser(UserToRegister);
            const string name = "Changed'; DROP TABLE users; --";
            await Repository.UpdateUser(ExistingUser.User.Id, name, null);
            Assert.That((await Repository.GetUserById(ExistingUser.User.Id))!.Name, Is.EqualTo(name));
            Assert.That((await Repository.GetUserById(UserToRegister.User.Id))!.Name, Is.EqualTo(UserToRegister.User.Name));
            Assert.That(await Repository.UpdateUser(Guid.NewGuid(), "Missing", null), Is.Zero);
        }

        [Test]
        public async Task FindsIdentityByIdWithoutCredentials()
        {
            var user = await Repository.GetUserById(ExistingUser.User.Id);
            Assert.That(user!.Id, Is.EqualTo(ExistingUser.User.Id));
            Assert.That(user.Name, Is.EqualTo(ExistingUser.User.Name));
            Assert.That(await Repository.GetUserById(Guid.NewGuid()), Is.Null);
        }

        [TestCase("User")]
        [TestCase("Manager")]
        [TestCase("Customer")]
        [TestCase("0")]
        [TestCase("Admin, Mechanic")]
        public async Task InvalidStoredRolePropagatesDomainValidation(string role)
        {
            await Connection.ExecuteAsync("UPDATE users SET role = @Role WHERE id = @Id", new { Role = role, Id = ExistingUser.User.Id });
            Assert.ThrowsAsync<DomainValidationException>(() => Repository.GetUser(ExistingUser.User.Name, role));
            Assert.ThrowsAsync<DomainValidationException>(() => Repository.GetUserCredentials(ExistingUser.User.Name, role));
        }

        [Test]
        public async Task EmptyStoredIdentityPropagatesDomainValidation()
        {
            await Connection.ExecuteAsync("UPDATE users SET id = @Id", new { Id = Guid.Empty });
            Assert.ThrowsAsync<DomainValidationException>(() => Repository.GetUserCredentials(ExistingUser.User.Name, "Admin"));
        }

        private IUserRepository Repository { get; set; }

        private static IUserCredentials UserToRegister { get; } = new UserCredentials(
            new User(Guid.NewGuid(), "Fulano", Roles.Mechanic.ToString()),
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
