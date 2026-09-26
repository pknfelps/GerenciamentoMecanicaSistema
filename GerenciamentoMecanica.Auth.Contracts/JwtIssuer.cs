using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GerenciamentoMecanica.Auth.Contracts;

/// <summary>Emite tokens sem dependência de configuração, persistência ou ASP.NET.</summary>
public sealed class JwtIssuer(string key, string? issuer, string? audience, TimeProvider? timeProvider = null)
{
    public const string Algorithm = SecurityAlgorithms.HmacSha256;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public string Generate(string name, string role, Guid? id = null)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role)
        ];
        // O consumidor fornece a identidade do cadastro; nunca o payload HTTP diretamente.
        if (id.HasValue)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, id.Value.ToString()));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), Algorithm);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: clock.GetUtcNow().UtcDateTime.Add(Lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
