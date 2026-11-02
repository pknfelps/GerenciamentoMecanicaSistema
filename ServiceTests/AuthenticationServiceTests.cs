using Domain.Interface.User;
using Domain.User;
using Service.Interface.Authentication;
using NSubstitute;
using NSubstitute.Extensions;
using Service.Interface.Persistence;
using Service;
using Service.Interface;
using Service.Interface.Commands.User;

namespace ServiceTests
{
    public class AuthenticationServiceTests
    {
        private const string GeneratedToken = "generated-token";
        private const string PasswordHash = "Stored-password-hash1!";

        private IAuthenticationService AuthenticationService { get; set; }
        private IUserRepository UsuarioRepository { get; set; }
        private ITokenGenerator TokenGenerator { get; set; }
        private IPasswordHasher PasswordHasher { get; set; }

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

        private static IUserCredentials ExistingCredentials { get; } =
            new UserCredentials(ExistingUser, PasswordHash);

        private static CreateUserCommand ExistingUserCommand { get; } = new("Admin", "Admin@123", "Admin");
        private static CreateUserCommand ExistingUserWithWrongPassword { get; } = new("Admin", "Teste@123", "Admin");
        private static CreateUserCommand UnexistingUser { get; } = new("Fulano", "Fulano@123", "Admin");

        [SetUp]
        public void SetUp()
        {
            UsuarioRepository = Substitute.For<IUserRepository>();
            TokenGenerator = Substitute.For<ITokenGenerator>();
            PasswordHasher = Substitute.For<IPasswordHasher>();

            UsuarioRepository.GetUserCredentials(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(0);
                var role = callInfo.ArgAt<string>(1);

                if (name == ExistingUser.Name && role == ExistingUser.Role.ToString())
                    return ExistingCredentials;

                return null;
            });

            TokenGenerator.Generate(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>()).Returns(GeneratedToken);
            PasswordHasher.Verify(ExistingUserCommand.Password, PasswordHash).Returns(true);

            AuthenticationService = new AuthenticationService(UsuarioRepository, TokenGenerator, PasswordHasher);
        }

        [Test]
        public async Task MustLogInAndGenerateToken()
        {
            var token = await AuthenticationService.Authenticate(ExistingUserCommand);

            Assert.That(token, Is.EqualTo(GeneratedToken));

            await UsuarioRepository.Received(1).GetUserCredentials(ExistingUser.Name, ExistingUser.Role.ToString());
            TokenGenerator.Received(1).Generate(ExistingUser.Name, ExistingUser.Role.ToString(), ExistingUser.Id);
            PasswordHasher.Received(1).Verify(ExistingUserCommand.Password, PasswordHash);
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioNotExists()
        {
            var token = await AuthenticationService.Authenticate(UnexistingUser);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioRepository.Received(1).GetUserCredentials(UnexistingUser.Name, UnexistingUser.Role);
            TokenGenerator.DidNotReceiveWithAnyArgs().Generate(default!, default!, Guid.Empty);
            PasswordHasher.DidNotReceiveWithAnyArgs().Verify(default!, default!);
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioTypedWrongPassword()
        {
            var token = await AuthenticationService.Authenticate(ExistingUserWithWrongPassword);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioRepository.Received(1).GetUserCredentials(ExistingUserWithWrongPassword.Name, ExistingUserWithWrongPassword.Role);
            TokenGenerator.DidNotReceiveWithAnyArgs().Generate(default!, default!, Guid.Empty);
            PasswordHasher.Received(1).Verify(ExistingUserWithWrongPassword.Password, PasswordHash);
        }
        
        [TestCase("User")]
        [TestCase("Manager")]
        [TestCase("Customer")]
        [TestCase("1")]
        [TestCase("Admin, Mechanic")]
        public async Task InvalidRoleDoesNotQueryCredentialsOrEmitToken(string role)
        {
            Assert.ThrowsAsync<Domain.Interface.Exceptions.DomainValidationException>(() =>
                AuthenticationService.Authenticate(new CreateUserCommand("Fulano", "Fulano@123", role)));
            await UsuarioRepository.DidNotReceiveWithAnyArgs().GetUserCredentials(default!, default!);
            TokenGenerator.DidNotReceiveWithAnyArgs().Generate(default!, default!, Guid.Empty);
        }

        [Test]
        public async Task MechanicUsesIdentityFromStoredCredentials()
        {
            var user = new User(Guid.NewGuid(), "Stored mechanic", "Mechanic");
            UsuarioRepository.Configure().GetUserCredentials("Requested name", "Mechanic")
                .Returns(new UserCredentials(user, PasswordHash));

            var token = await AuthenticationService.Authenticate(new CreateUserCommand("Requested name", ExistingUserCommand.Password, "Mechanic"));

            Assert.That(token, Is.EqualTo(GeneratedToken));
            TokenGenerator.Received(1).Generate(user.Name, "Mechanic", user.Id);
        }
    }
}
