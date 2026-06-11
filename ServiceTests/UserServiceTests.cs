using Domain.Interface.User;
using Domain.User;
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

        private static readonly CreateUserDto UserToRegister = new("Fulano", "Senha@123", "Manager");
        private static readonly CreateUserDto UserToFail = new("Teste", "Teste@123", "User");
        private static readonly List<IUser> ExistingUsers =
        [
            new User("Admin", "Admin@123", "Admin")
        ];
        private static readonly UserDto ExistingUserDto = new(Guid.NewGuid(), "Admin", "Admin@123", "Admin");

        [SetUp]
        public void SetUp()
        {
            UserRepository = Substitute.For<IUserRepository>();

            UserRepository.RegisterUser(Arg.Any<IUser>()).Returns(callInfo =>
            {
                var User = callInfo.ArgAt<IUser>(0);

                if (User.Name == UserToRegister.Name && User.Password.Secret == UserToRegister.Password && User.Role.ToString() == UserToRegister.Role)
                    return 1;

                if (User.Name == UserToFail.Name && User.Password.Secret == UserToFail.Password && User.Role.ToString() == UserToFail.Role)
                    throw new InvalidOperationException();

                return 0;
            });

            UserRepository.GetUser(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var nome = callInfo.ArgAt<string>(0);
                var cargo = callInfo.ArgAt<string>(1);

                return ExistingUsers.FirstOrDefault(x => x.Name == nome && x.Role.ToString() == cargo);
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
