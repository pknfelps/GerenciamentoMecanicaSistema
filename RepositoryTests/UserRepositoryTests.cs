using Dapper;
using Domain.Interface.User;
using NSubstitute;
using Repository;
using Repository.Interface;

namespace RepositoryTests
{
    public class UserRepositoryTests : BaseRepositoryTests
    {
        private IUserRepository Repository { get; set; }

        private static IUser UserToRegister
        {
            get
            {
                var user = Substitute.For<IUser>();
                user.Id.Returns(Guid.NewGuid());
                user.Name.Returns("Fulano");
                user.Password.Secret.Returns("Senha@123");
                user.Role.Returns(Roles.Manager);
                return user;
            }
        }

        private static IUser ExistingUser
        {
            get
            {
                var user = Substitute.For<IUser>();
                user.Id.Returns(Guid.NewGuid());
                user.Name.Returns("Admin");
                user.Password.Secret.Returns("Admin@123");
                user.Role.Returns(Roles.Admin);
                return user;
            }
        }

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
            var User = await Repository.GetUser(ExistingUser.Name, ExistingUser.Role.ToString());

            Assert.That(User, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(User.Name, Is.EqualTo(ExistingUser.Name));
                Assert.That(User.Role, Is.EqualTo(ExistingUser.Role));
            });
        }

        [Test]
        public async Task MustNotGetUserByNomeAndCargoIfNotExists()
        {
            var User = await Repository.GetUser(UserToRegister.Name, UserToRegister.Role.ToString());

            Assert.That(User, Is.Null);
        }
    }
}
