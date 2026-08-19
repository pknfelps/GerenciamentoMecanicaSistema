using Infrastructure.Interface.Authentication;
using Infrastructure.Interface.Persistence;
using Service.Interface;
using Service.Interface.Commands.User;

namespace Service
{
    public class AuthenticationService(IUserRepository userRepository, ITokenGenerator tokenGenerator, IPasswordHasher passwordHasher) : IAuthenticationService
    {
        private IUserRepository UserRepository { get; set; } = userRepository;
        private ITokenGenerator TokenGenerator { get; set; } = tokenGenerator;
        private IPasswordHasher PasswordHasher { get; set; } = passwordHasher;

        public async Task<string> Authenticate(CreateUserCommand user)
        {
            var credentials = await UserRepository.GetUserCredentials(user.Name, user.Role);

            if (credentials == null)
                return string.Empty;

            if (!PasswordHasher.Verify(user.Password, credentials.PasswordHash))
                return string.Empty;

            return TokenGenerator.Generate(credentials.User.Name, credentials.User.Role.ToString());
        }
    }
}
