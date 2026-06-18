using Domain.Interface.User;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto.User;

namespace ServiceTests
{
    public class UserServiceTests
    {
        private IUserService UserService { get; set; }
        private IUserRepository UserRepository { get; set; }

        private static CreateUserDto UserToRegister { get; } = new("Fulano", "Senha@123", "Manager");
        private static CreateUserDto UserToFail { get; } = new("Teste", "Teste@123", "User");

        private static readonly Guid ExistingUserId = Guid.NewGuid();
        private static IUser ExistingUser
        {
            get
            {
                var user = Substitute.For<IUser>();
                user.Id.Returns(ExistingUserId);
                user.Name.Returns("Admin");
                user.Password.Secret.Returns("Admin@123");
                user.Role.Returns(Roles.Admin);
                return user;
            }
        }

        private static UserDto ExistingUserDto { get; } = new(Guid.NewGuid(), "Admin", "Admin@123", "Admin");

        [SetUp]
        public void SetUp()
        {
            UserRepository = Substitute.For<IUserRepository>();

            UserRepository.RegisterUser(Arg.Any<IUser>()).Returns(callInfo =>
            {
                var user = callInfo.ArgAt<IUser>(0);

                if (user.Name == UserToRegister.Name && user.Password.Secret == UserToRegister.Password && user.Role.ToString() == UserToRegister.Role)
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

            UserService = new UserService(UserRepository);
        }

        [Test]
        public async Task MustCreateUser()
        {
            await UserService.RegisterUser(UserToRegister);

            await UserRepository.ReceivedWithAnyArgs(1).RegisterUser(Arg.Any<IUser>());
        }

        [Test]
        public async Task MustNotCreateUserIfExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await UserService.RegisterUser(ExistingUserDto));

            await UserRepository.ReceivedWithAnyArgs(0).RegisterUser(Arg.Any<IUser>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailRegister()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await UserService.RegisterUser(UserToFail));

            await UserRepository.ReceivedWithAnyArgs(1).RegisterUser(Arg.Any<IUser>());
        }

        [Test]
        public async Task MustGetUser()
        {
            var User = await UserService.GetUser(ExistingUserDto);

            await UserRepository.ReceivedWithAnyArgs(1).GetUser(Arg.Any<string>(), Arg.Any<string>());

            Assert.That(User, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(User.Name, Is.EqualTo(ExistingUserDto.Name));
                Assert.That(User.Role, Is.EqualTo(ExistingUserDto.Role));
            });
        }

        [Test]
        public async Task MustNotGetUserIfNotExists()
        {
            var User = await UserService.GetUser(UserToRegister);

            await UserRepository.ReceivedWithAnyArgs(1).GetUser(Arg.Any<string>(), Arg.Any<string>());

            Assert.That(User, Is.Null);
        }
    }
}
