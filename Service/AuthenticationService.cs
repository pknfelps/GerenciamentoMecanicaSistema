using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Service
{
    public class AuthenticationService(IConfiguration configuration, IUserRepository userRepository) : IAuthenticationService
    {
        private IConfiguration Configuration { get; set; } = configuration;
        private IUserRepository UserRepository { get; set; } = userRepository;

        public async Task<string> Authenticate(CreateUserDto userDto)
        {
            var user = await UserRepository.GetUser(userDto.Name, userDto.Role);

            if (user == null)
                return string.Empty;

            if (userDto.Password != user.Password.Secret)
                return string.Empty;

            SymmetricSecurityKey secretKey = new(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"] ?? string.Empty));
            var issuer = Configuration["Jwt:Issuer"];
            var audience = Configuration["Jwt:Audience"];

            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var tokenOptions = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims:
                [
                    new Claim(type: ClaimTypes.Name, user.Name),
                    new Claim(type: ClaimTypes.Role, user.Role.ToString())
                ],
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: signinCredentials);

            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }
    }
}
