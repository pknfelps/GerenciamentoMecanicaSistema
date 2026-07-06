using Repository.Interface;
using Service.Interface;
using Service.Interface.Commands.User;

namespace Service
{
    public class AuthenticationService(IUserRepository userRepository, ITokenGenerator tokenGenerator) : IAuthenticationService
    {
        private IUserRepository UserRepository { get; set; } = userRepository;
        private ITokenGenerator TokenGenerator { get; set; } = tokenGenerator;

        public async Task<string> Authenticate(CreateUserCommand user)
        {
            var registeredUser = await UserRepository.GetUser(user.Name, user.Role);

            if (registeredUser == null)
                return string.Empty;

            if (user.Password != registeredUser.Password.Secret)
                return string.Empty;

            return TokenGenerator.Generate(registeredUser.Name, registeredUser.Role.ToString());
        }
    }
}
