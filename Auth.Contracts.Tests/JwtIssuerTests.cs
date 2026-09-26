using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GerenciamentoMecanica.Auth.Contracts;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Contracts.Tests;

public class JwtIssuerTests
{
    private const string TestKey = "test-only-signing-key-with-at-least-32-characters";
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void PreservesLegacyWireFormatExactly()
    {
        var actual = new JwtIssuer(TestKey, "admin", "mecanica", new FixedClock(Now)).Generate("oficina", "Admin");
        // Baseline do gerador anterior, com relógio fixado para comparação byte a byte.
        var legacy = new JwtSecurityToken("admin", "mecanica",
            [new Claim(ClaimTypes.Name, "oficina"), new Claim(ClaimTypes.Role, "Admin")],
            expires: Now.UtcDateTime.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)), SecurityAlgorithms.HmacSha256));
        Assert.That(actual, Is.EqualTo(new JwtSecurityTokenHandler().WriteToken(legacy)));
        var decoded = new JwtSecurityTokenHandler().ReadJwtToken(actual);
        Assert.That(decoded.ValidTo, Is.EqualTo(Now.UtcDateTime.AddMinutes(10)));
        Assert.That(decoded.Header.Alg, Is.EqualTo("HS256"));
        Assert.That(decoded.Claims.Any(c => c.Type == ClaimTypes.NameIdentifier), Is.False);
    }

    [TestCase("Admin")]
    [TestCase("Mechanic")]
    [TestCase("Customer")]
    public void SupportsStableIdentityWithoutChangingNameAndRole(string role)
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var token = new JwtIssuer(TestKey, "admin", "mecanica").Generate("cadastro", role, id);
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = "admin",
            ValidateAudience = true, ValidAudience = "mecanica",
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey))
        }, out _);
        Assert.That(principal.Identity!.Name, Is.EqualTo("cadastro"));
        Assert.That(principal.IsInRole(role), Is.True);
        Assert.That(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value, Is.EqualTo(id.ToString()));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
