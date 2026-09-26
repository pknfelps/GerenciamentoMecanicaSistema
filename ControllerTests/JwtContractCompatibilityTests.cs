using System.Security.Claims;
using GerenciamentoMecanica.Auth.Contracts;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ControllerTests;

public class JwtContractCompatibilityTests
{
    private TestWebApplicationFactory factory = null!;
    private JwtBearerOptions options = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        factory = new TestWebApplicationFactory();
        options = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
    }

    [OneTimeTearDown]
    public void TearDown() => factory.Dispose();

    [TestCase("Admin")]
    [TestCase("Mechanic")]
    [TestCase("Customer")]
    public async Task SharedTokenIsAcceptedByActualApiConfiguration(string role)
    {
        var id = Guid.NewGuid();
        var token = CreateIssuer().Generate("cadastro", role, id);
        var result = await options.TokenHandlers[0].ValidateTokenAsync(token, options.TokenValidationParameters);
        Assert.That(result.IsValid, Is.True, result.Exception?.Message);
        Assert.That(result.ClaimsIdentity.Name, Is.EqualTo("cadastro"));
        Assert.That(result.ClaimsIdentity.FindFirst(ClaimTypes.Role)!.Value, Is.EqualTo(role));
        Assert.That(result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)!.Value, Is.EqualTo(id.ToString()));
    }

    [Test]
    public async Task ExistingInfrastructureAdapterIsStillAccepted()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = TestWebApplicationFactory.JwtKey,
            ["Jwt:Issuer"] = TestWebApplicationFactory.JwtIssuer,
            ["Jwt:Audience"] = TestWebApplicationFactory.JwtAudience
        }).Build();
        var token = new JwtTokenGenerator(configuration).Generate("oficina", "Admin");
        var result = await options.TokenHandlers[0].ValidateTokenAsync(token, options.TokenValidationParameters);
        Assert.That(result.IsValid, Is.True, result.Exception?.Message);
        Assert.That(result.ClaimsIdentity.Name, Is.EqualTo("oficina"));
        Assert.That(result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier), Is.Null);
    }

    [TestCase("key")]
    [TestCase("issuer")]
    [TestCase("audience")]
    [TestCase("expired")]
    public async Task ApiRejectsInvalidToken(string invalidField)
    {
        var token = new JwtIssuer(
            invalidField == "key" ? new string('x', 64) : TestWebApplicationFactory.JwtKey,
            invalidField == "issuer" ? "other" : TestWebApplicationFactory.JwtIssuer,
            invalidField == "audience" ? "other" : TestWebApplicationFactory.JwtAudience,
            new OffsetClock(invalidField == "expired" ? -16 : 0)).Generate("cadastro", "Customer");
        var result = await options.TokenHandlers[0].ValidateTokenAsync(token, options.TokenValidationParameters);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task PreservesApiClockSkew()
    {
        Assert.That(options.TokenValidationParameters.ClockSkew, Is.EqualTo(TimeSpan.FromMinutes(5)));
        var token = CreateIssuer(new OffsetClock(-14)).Generate("cadastro", "Admin");
        var result = await options.TokenHandlers[0].ValidateTokenAsync(token, options.TokenValidationParameters);
        Assert.That(result.IsValid, Is.True);
    }

    private static JwtIssuer CreateIssuer(TimeProvider? clock = null) =>
        new(TestWebApplicationFactory.JwtKey, TestWebApplicationFactory.JwtIssuer,
            TestWebApplicationFactory.JwtAudience, clock);

    private sealed class OffsetClock(int minutes) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddMinutes(minutes);
    }
}
