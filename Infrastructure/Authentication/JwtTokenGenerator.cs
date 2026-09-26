using GerenciamentoMecanica.Auth.Contracts;
using Microsoft.Extensions.Configuration;
using Service.Interface.Authentication;

namespace Infrastructure.Authentication
{
    public class JwtTokenGenerator(IConfiguration configuration) : ITokenGenerator
    {
        public string Generate(string userName, string role) =>
            new JwtIssuer(
                configuration["Jwt:Key"] ?? string.Empty,
                configuration["Jwt:Issuer"],
                configuration["Jwt:Audience"])
            .Generate(userName, role);
    }
}
