using Dapper;
using Domain.Interface.User;
using Domain.User;
using Repository;
using Repository.Interface;

namespace RepositoryTests
{
    public class UserRepositoryTests : BaseRepositoryTests
    {
        private IUserRepository Repository { get; set; }

        private static readonly IUser UserToRegister = new User("Fulano", "Senha@123", "Manager");
        private static readonly List<IUser> ExistingUsers =
        [
            new User("Admin", "Admin@123", "Admin")
        ];

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

            foreach (IUser User in ExistingUsers)
                await Repository.RegisterUser(User);
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
            var User = await Repository.GetUser(ExistingUsers[0].Name, ExistingUsers[0].Role.ToString());

            Assert.That(User, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(User.Name, Is.EqualTo(ExistingUsers[0].Name));
                Assert.That(User.Role, Is.EqualTo(ExistingUsers[0].Role));
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
