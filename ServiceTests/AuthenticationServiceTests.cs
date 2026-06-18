using Domain.Interface.User;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto.User;

namespace ServiceTests
{
    public class AuthenticationServiceTests
    {
        private IAuthenticationService AuthenticationService { get; set; }
        private IUserRepository UsuarioRepository { get; set; }
        private IConfiguration Configuration { get; set; }

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

        private static CreateUserDto ExistingUserDto { get; } = new("Admin", "Admin@123", "Admin");
        private static CreateUserDto ExistingUserWithWrongPassword { get; } = new("Admin", "Teste@123", "Admin");
        private static CreateUserDto UnexistingUser { get; } = new("Fulano", "Fulano@123", "Usuario");

        [SetUp]
        public void SetUp()
        {
            UsuarioRepository = Substitute.For<IUserRepository>();
            Configuration = Substitute.For<IConfiguration>();

            UsuarioRepository.GetUser(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(0);
                var role = callInfo.ArgAt<string>(1);

                if (name == ExistingUser.Name && role == ExistingUser.Role.ToString())
                    return ExistingUser;

                return null;
            });

            Configuration["Jwt:Key"].Returns("chaveTestesecurityKeyfortestingTokengeneration");
            Configuration["Jwt:Issuer"].Returns("admin");
            Configuration["Jwt:Audience"].Returns("mecanica");

            AuthenticationService = new AuthenticationService(Configuration, UsuarioRepository);
        }

        [Test]
        public async Task MustLogInAndGenerateToken()
        {
            var token = await AuthenticationService.Authenticate(ExistingUserDto);

            Assert.That(string.IsNullOrEmpty(token), Is.False);

            await UsuarioRepository.Received(1).GetUser(ExistingUser.Name, ExistingUser.Role.ToString());
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioNotExists()
        {
            var token = await AuthenticationService.Authenticate(UnexistingUser);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioRepository.Received(1).GetUser(UnexistingUser.Name, UnexistingUser.Role);
        }

        [Test]
        public async Task MustNotLogInAndNotGenerateTokenIfUsuarioTypedWrongPassword()
        {
            var token = await AuthenticationService.Authenticate(ExistingUserWithWrongPassword);

            Assert.That(string.IsNullOrEmpty(token), Is.True);

            await UsuarioRepository.Received(1).GetUser(ExistingUserWithWrongPassword.Name, ExistingUserWithWrongPassword.Role);
        }
    }
}
