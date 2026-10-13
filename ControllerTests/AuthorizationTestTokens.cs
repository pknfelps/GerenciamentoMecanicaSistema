using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ControllerTests;

public static class AuthorizationTestTokens
{
    public static string Create(string role, string? id, bool duplicateId = false, string name = "test identity", DateTime? expires = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, name), new(ClaimTypes.Role, role) };
        if (id is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, id));
            if (duplicateId) claims.Add(new Claim(ClaimTypes.NameIdentifier, id));
        }
        var token = new JwtSecurityToken(TestWebApplicationFactory.JwtIssuer, TestWebApplicationFactory.JwtAudience,
            claims, expires: expires ?? DateTime.UtcNow.AddMinutes(10), signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestWebApplicationFactory.JwtKey)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
