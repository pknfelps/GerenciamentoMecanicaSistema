using Domain.Interface.User;
using Domain.Interface.Exceptions;
using Service.Interface.Authentication;
using NSubstitute;
using Service.Interface.Persistence;
using Service;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.User;
using Service.Interface.Results.User;

namespace ServiceTests
{
    public class UserServiceTests
    {
        [Test]
        public async Task OwnAccountUsesIdAndReturnsCurrentName()
        {
            var user = new Domain.User.User(Guid.NewGuid(), "Current name", "Mechanic");
            UserRepository.GetUserById(user.Id).Returns(user);
            var result = await UserService.GetOwnUser(user.Id);
            Assert.That(result, Is.EqualTo(new UserResult(user.Id, user.Name, "Mechanic")));
            await UserRepository.DidNotReceiveWithAnyArgs().GetUser(default!, default!);
        }

        [TestCase("Another name", "Mechanic")]
        [TestCase("Current name", "Admin")]
        public async Task OwnAccountRejectsConflictingFiltersWithoutLookingUpOthers(string name, string role)
        {
            var user = new Domain.User.User(Guid.NewGuid(), "Current name", "Mechanic");
            UserRepository.GetUserById(user.Id).Returns(user);
            Assert.ThrowsAsync<AccessDeniedException>(() => UserService.GetOwnUser(user.Id, name, role));
            await UserRepository.Received(1).GetUserById(user.Id);
            await UserRepository.DidNotReceiveWithAnyArgs().GetUser(default!, default!);
        }

        [Test]
        public async Task OwnAccountReturnsNullWhenDeleted()
        {
            var userId = Guid.NewGuid();
            UserRepository.GetUserById(userId).Returns((IUser?)null);
            Assert.That(await UserService.GetOwnUser(userId), Is.Null);
        }

        private IUserService UserService { get; set; }
        private IUserRepository UserRepository { get; set; }
        private IPasswordHasher PasswordHasher { get; set; }
        private const string PasswordHash = "Stored-password-hash1!";

        private static CreateUserCommand UserToRegister { get; } = new("Fulano", "Senha@123", "Mechanic");
        private static CreateUserCommand UserToFail { get; } = new("Teste", "Teste@123", "Mechanic");

        private static readonly Guid ExistingUserId = Guid.NewGuid();
        private static IUser ExistingUser
        {
            get
            {
                var user = Substitute.For<IUser>();
                user.Id.Returns(ExistingUserId);
                user.Name.Returns("Admin");
                user.Role.Returns(Roles.Admin);
                return user;
            }
        }

        private static UserResult ExistingUserResult { get; } = new(Guid.NewGuid(), "Admin", "Admin");
        private static CreateUserCommand ExistingUserCommand { get; } = new("Admin", "Admin@123", "Admin");

        [SetUp]
        public void SetUp()
        {
            UserRepository = Substitute.For<IUserRepository>();
            PasswordHasher = Substitute.For<IPasswordHasher>();
            PasswordHasher.Hash(Arg.Any<string>()).Returns(PasswordHash);

            UserRepository.RegisterUser(Arg.Any<IUserCredentials>()).Returns(callInfo =>
            {
                var credentials = callInfo.ArgAt<IUserCredentials>(0);
                var user = credentials.User;

                if (user.Name == UserToRegister.Name
                    && credentials.PasswordHash == PasswordHash
                    && user.Role.ToString() == UserToRegister.Role)
                    return 1;

                return 0;
            });

            UserRepository.GetUser(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(0);
                var role = callInfo.ArgAt<string>(1);

                if (name == ExistingUser.Name && role == ExistingUser.Role.ToString())
                    return ExistingUser;

                return null;
            });

            UserService = new UserService(UserRepository, PasswordHasher);
        }

        [Test]
        public async Task MustCreateUser()
        {
            await UserService.RegisterUser(UserToRegister);

            await UserRepository.ReceivedWithAnyArgs(1).RegisterUser(Arg.Any<IUserCredentials>());
            PasswordHasher.Received(1).Hash(UserToRegister.Password);
        }

        [Test]
        public void MustNotHashOrRegisterUserWithInvalidPassword()
        {
            var invalidUser = new CreateUserCommand("Fulano", "invalid", "Mechanic");

            Assert.CatchAsync<DomainValidationException>(() => UserService.RegisterUser(invalidUser));

            PasswordHasher.DidNotReceiveWithAnyArgs().Hash(default!);
            UserRepository.DidNotReceiveWithAnyArgs().RegisterUser(default!);
        }

        [Test]
        public async Task MustNotCreateUserIfExists()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await UserService.RegisterUser(ExistingUserCommand));

            await UserRepository.ReceivedWithAnyArgs(0).RegisterUser(Arg.Any<IUserCredentials>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailRegister()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await UserService.RegisterUser(UserToFail));

            await UserRepository.ReceivedWithAnyArgs(1).RegisterUser(Arg.Any<IUserCredentials>());
        }

        [Test]
        public async Task MustGetUser()
        {
            var User = await UserService.GetUser(ExistingUserCommand.Name, ExistingUserCommand.Role);

            await UserRepository.ReceivedWithAnyArgs(1).GetUser(Arg.Any<string>(), Arg.Any<string>());

            Assert.That(User, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(User.Name, Is.EqualTo(ExistingUserResult.Name));
                Assert.That(User.Role, Is.EqualTo(ExistingUserResult.Role));
            });
        }

        [Test]
        public async Task MustNotGetUserIfNotExists()
        {
            var User = await UserService.GetUser(UserToRegister.Name, UserToRegister.Role);

            await UserRepository.ReceivedWithAnyArgs(1).GetUser(Arg.Any<string>(), Arg.Any<string>());

            Assert.That(User, Is.Null);
        }
        
        [TestCase("User")]
        [TestCase("Manager")]
        [TestCase("Customer")]
        [TestCase("1")]
        public async Task InvalidRoleDoesNotQueryOrWriteUser(string role)
        {
            Assert.ThrowsAsync<DomainValidationException>(() => UserService.RegisterUser(new CreateUserCommand("Fulano", "Fulano@123", role)));
            await UserRepository.DidNotReceiveWithAnyArgs().GetUser(default!, default!);
            await UserRepository.DidNotReceiveWithAnyArgs().RegisterUser(default!);
            PasswordHasher.DidNotReceiveWithAnyArgs().Hash(default!);
        }
    }
}

