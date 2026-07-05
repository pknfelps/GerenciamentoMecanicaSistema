using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto.User;

namespace Service
{
    public class AuthenticationService(IUserRepository userRepository, ITokenGenerator tokenGenerator) : IAuthenticationService
    {
        private IUserRepository UserRepository { get; set; } = userRepository;
        private ITokenGenerator TokenGenerator { get; set; } = tokenGenerator;

        public async Task<string> Authenticate(CreateUserDto userDto)
        {
            var user = await UserRepository.GetUser(userDto.Name, userDto.Role);

            if (user == null)
                return string.Empty;

            if (userDto.Password != user.Password.Secret)
                return string.Empty;

            return TokenGenerator.Generate(user.Name, user.Role.ToString());
        }
    }
}
