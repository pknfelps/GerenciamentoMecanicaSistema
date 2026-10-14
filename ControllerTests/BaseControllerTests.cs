using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace ControllerTests
{
    public abstract class BaseControllerTests
    {
        protected TestWebApplicationFactory TestWebAppFactory { get; set; }
        protected HttpClient TestClient { get; set; }
        protected virtual string TestRole => "Mechanic";
        protected virtual TestWebApplicationFactory CreateFactory() => new();
        protected Guid TestIdentityId { get; } = Guid.NewGuid();

        protected void UseToken(string role) => TestClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestToken(role));

        [SetUp]
        public virtual Task SetUp()
        {
            TestWebAppFactory = CreateFactory();
            TestClient = TestWebAppFactory.CreateClient();
            UseToken(TestRole);

            MockService();
            
            return Task.CompletedTask;
        }

        protected abstract void MockService();

        [TearDown]
        public virtual Task TearDown()
        {
            TestClient.Dispose();
            TestWebAppFactory.Dispose();
            return Task.CompletedTask;
        }

        private string GenerateTestToken(string role)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, role),
                new(ClaimTypes.Role, role),
                new(ClaimTypes.NameIdentifier, TestIdentityId.ToString())
            };

            var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestWebApplicationFactory.JwtKey));

            var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: TestWebApplicationFactory.JwtIssuer,
                audience: TestWebApplicationFactory.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: credenciais
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
