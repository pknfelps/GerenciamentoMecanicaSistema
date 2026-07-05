using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Service.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Authentication
{
    public class JwtTokenGenerator(IConfiguration configuration) : ITokenGenerator
    {
        private IConfiguration Configuration { get; set; } = configuration;

        public string Generate(string userName, string role)
        {
            SymmetricSecurityKey secretKey = new(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"] ?? string.Empty));
            var issuer = Configuration["Jwt:Issuer"];
            var audience = Configuration["Jwt:Audience"];

            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var tokenOptions = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims:
                [
                    new Claim(type: ClaimTypes.Name, userName),
                    new Claim(type: ClaimTypes.Role, role)
                ],
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: signinCredentials);

            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }
    }
}
