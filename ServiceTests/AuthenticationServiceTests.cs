using Domain.Interface.User;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Commands.User;

namespace ServiceTests
{
    public class AuthenticationServiceTests
    {
        private const string GeneratedToken = "generated-token";

        private IAuthenticationService AuthenticationService { get; set; }
        private IUserRepository UsuarioRepository { get; set; }
        private ITokenGenerator TokenGenerator { get; set; }

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

        private static CreateUserCommand ExistingUserCommand { get; } = new("Admin", "Admin@123", "Admin");
        private static CreateUserCommand ExistingUserWithWrongPassword { get; } = new("Admin", "Teste@123", "Admin");
        private static CreateUserCommand UnexistingUser { get; } = new("Fulano", "Fulano@123", "Usuario");

        [SetUp]
        public void SetUp()
        {
            UsuarioRepository = Substitute.For<IUserRepository>();
            TokenGenerator = Substitute.For<ITokenGenerator>();

            UsuarioRepository.GetUser(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(0);
                var role = callInfo.ArgAt<string>(1);

                if (name == ExistingUser.Name && role == ExistingUser.Role.ToString())
                    return ExistingUser;

                return null;
            });

            TokenGenerator.Generate(Arg.Any<string>(), Arg.Any<string>()).Returns(GeneratedToken);

            AuthenticationService = new AuthenticationService(UsuarioRepository, TokenGenerator);
        }

        [Test]
        public async Task MustLogInAndGenerateToken()
        {
            var token = await AuthenticationService.Authenticate(ExistingUserCommand);

            Assert.That(token, Is.EqualTo(GeneratedToken));

            await UsuarioRepository.Received(1).GetUser(ExistingUser.Name, ExistingUser.Role.ToString());
            TokenGenerator.Received(1).Generate(ExistingUser.Name, ExistingUser.Role.ToString());
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioNotExists()
        {
            var token = await AuthenticationService.Authenticate(UnexistingUser);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioRepository.Received(1).GetUser(UnexistingUser.Name, UnexistingUser.Role);
            TokenGenerator.DidNotReceiveWithAnyArgs().Generate(default!, default!);
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioTypedWrongPassword()
        {
            var token = await AuthenticationService.Authenticate(ExistingUserWithWrongPassword);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioRepository.Received(1).GetUser(ExistingUserWithWrongPassword.Name, ExistingUserWithWrongPassword.Role);
            TokenGenerator.DidNotReceiveWithAnyArgs().Generate(default!, default!);
        }
    }
}
