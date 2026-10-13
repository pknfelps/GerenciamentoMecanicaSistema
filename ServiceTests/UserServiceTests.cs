using Domain.Interface.User;
using Domain.Interface.Exceptions;
using Service.Interface.Authentication;
using NSubstitute;
using NSubstitute.Extensions;
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
        public async Task DeletesOnlyRequestedIdentityWithoutLoadingAccount()
        {
            UserRepository.DeleteUser(ExistingUserId).Returns(1);
            await UserService.DeleteUser(ExistingUserId);
            await UserRepository.Received(1).DeleteUser(ExistingUserId);
            await UserRepository.DidNotReceiveWithAnyArgs().GetUserById(Guid.Empty);
            await UserRepository.DidNotReceiveWithAnyArgs().GetUser(default!, default!);
            await UserRepository.DidNotReceiveWithAnyArgs().GetUserCredentials(default!, default!);
        }

        [Test]
        public async Task MissingDeletedUserIsNotFound()
        {
            Assert.ThrowsAsync<NotFoundException>(() => UserService.DeleteUser(ExistingUserId));
            await UserRepository.Received(1).DeleteUser(ExistingUserId);
        }

        [Test]
        public async Task EmptyDeletedIdentityDoesNotCallRepository()
        {
            Assert.ThrowsAsync<InvalidRequestException>(() => UserService.DeleteUser(Guid.Empty));
            await UserRepository.DidNotReceiveWithAnyArgs().DeleteUser(Guid.Empty);
        }

        [Test]
        public void DeleteFailurePropagatesAsTechnicalFailure()
        {
            UserRepository.DeleteUser(ExistingUserId).Returns(Task.FromException<int>(new InvalidOperationException("test failure")));
            Assert.ThrowsAsync<InvalidOperationException>(() => UserService.DeleteUser(ExistingUserId));
        }

        private Domain.User.User PrepareOwnAccount()
        {
            var user = new Domain.User.User(ExistingUserId, "Original mechanic", "Mechanic");
            UserRepository.GetUserById(user.Id).Returns(user);
            UserRepository.UpdateUser(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>()).Returns(1);
            return user;
        }

        [TestCase("Changed mechanic", null)]
        [TestCase(null, "Changed@Test123")]
        [TestCase("Changed mechanic", "Changed@Test123")]
        public async Task UpdateOwnAccountPreservesOmittedFields(string? name, string? password)
        {
            var user = PrepareOwnAccount();
            await UserService.UpdateOwnUser(user.Id, new(name, password));
            await UserRepository.Received(1).GetUserById(user.Id);
            await UserRepository.Received(1).UpdateUser(user.Id, name, password is null ? null : PasswordHash);
            PasswordHasher.Received(password is null ? 0 : 1).Hash(Arg.Any<string>());
            if (name is null) await UserRepository.DidNotReceiveWithAnyArgs().GetUser(default!, default!);
            else await UserRepository.Received(1).GetUser(name, "Mechanic");
        }

        [Test]
        public async Task OwnNameIsNotAConflict()
        {
            var user = PrepareOwnAccount();
            UserRepository.Configure().GetUser(user.Name, "Mechanic").Returns(user);
            await UserService.UpdateOwnUser(user.Id, new(user.Name, null));
            await UserRepository.Received(1).UpdateUser(user.Id, user.Name, null);
        }

        [Test]
        public async Task AnotherAccountNameIsAConflictWithoutHashingOrWriting()
        {
            var user = PrepareOwnAccount();
            UserRepository.Configure().GetUser("Other mechanic", "Mechanic").Returns(new Domain.User.User("Other mechanic", "Mechanic"));
            Assert.ThrowsAsync<ConflictException>(() => UserService.UpdateOwnUser(user.Id, new("Other mechanic", "Changed@Test123")));
            await UserRepository.DidNotReceiveWithAnyArgs().UpdateUser(Guid.Empty, null, null);
            PasswordHasher.DidNotReceiveWithAnyArgs().Hash(default!);
        }

        [TestCase(null, null)]
        [TestCase("", null)]
        [TestCase("   ", null)]
        public async Task InvalidUpdateDoesNotQueryOrWrite(string? name, string? password)
        {
            Assert.ThrowsAsync<InvalidRequestException>(() => UserService.UpdateOwnUser(ExistingUserId, new(name, password)));
            await UserRepository.DidNotReceiveWithAnyArgs().GetUserById(Guid.Empty);
            await UserRepository.DidNotReceiveWithAnyArgs().UpdateUser(Guid.Empty, null, null);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("invalid")]
        [TestCase("NoDigit@")]
        [TestCase("NoSymbol123")]
        public async Task InvalidNewPasswordDoesNotHashOrWrite(string password)
        {
            var user = PrepareOwnAccount();
            Assert.ThrowsAsync<DomainValidationException>(() => UserService.UpdateOwnUser(user.Id, new(null, password)));
            await UserRepository.DidNotReceiveWithAnyArgs().UpdateUser(Guid.Empty, null, null);
            PasswordHasher.DidNotReceiveWithAnyArgs().Hash(default!);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task MissingAccountBeforeOrDuringUpdateIsNotFound(bool deletedDuringUpdate)
        {
            var userId = ExistingUserId;
            if (deletedDuringUpdate)
            {
                PrepareOwnAccount();
                UserRepository.UpdateUser(userId, "Changed mechanic", null).Returns(0);
            }
            else UserRepository.GetUserById(userId).Returns((IUser?)null);
            Assert.ThrowsAsync<NotFoundException>(() => UserService.UpdateOwnUser(userId, new("Changed mechanic", null)));
            if (!deletedDuringUpdate) await UserRepository.DidNotReceiveWithAnyArgs().UpdateUser(Guid.Empty, null, null);
        }

        [Test]
        public async Task EmptyIdentityAndOverlongNameDoNotUpdate()
        {
            Assert.ThrowsAsync<InvalidRequestException>(() => UserService.UpdateOwnUser(Guid.Empty, new("Changed mechanic", null)));
            Assert.ThrowsAsync<InvalidRequestException>(() => UserService.UpdateOwnUser(ExistingUserId, new(new string('a', 101), null)));
            await UserRepository.DidNotReceiveWithAnyArgs().UpdateUser(Guid.Empty, null, null);
        }

        [Test]
        public async Task UnexpectedFailurePropagatesWithoutWriting()
        {
            var user = PrepareOwnAccount();
            PasswordHasher.Hash("Changed@Test123").Returns(_ => throw new InvalidOperationException("test failure"));
            Assert.ThrowsAsync<InvalidOperationException>(() => UserService.UpdateOwnUser(user.Id, new(null, "Changed@Test123")));
            await UserRepository.DidNotReceiveWithAnyArgs().UpdateUser(Guid.Empty, null, null);
        }

        [Test]
        public void UpdateCommandDoesNotExposePasswordInToString()
        {
            var command = new UpdateOwnUserCommand("Changed mechanic", "Changed@Test123");
            Assert.That(command.ToString(), Does.Not.Contain(command.Password));
        }

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

