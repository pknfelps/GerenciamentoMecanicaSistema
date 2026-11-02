using GerenciamentoMecanica.Auth.Contracts;
using Microsoft.Extensions.Configuration;
using Service.Interface.Authentication;
using Domain.User;

namespace Infrastructure.Authentication
{
    public class JwtTokenGenerator(IConfiguration configuration) : ITokenGenerator
    {
        public string Generate(string userName, string role, Guid userId)
        {
            var user = new User(userId, userName, role);

            return new JwtIssuer(
                configuration["Jwt:Key"] ?? string.Empty,
                configuration["Jwt:Issuer"],
                configuration["Jwt:Audience"])
                .Generate(user.Name, user.Role.ToString(), user.Id);
        }
    }
}
