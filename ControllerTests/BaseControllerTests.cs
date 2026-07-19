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

        [SetUp]
        public virtual Task SetUp()
        {
            TestWebAppFactory = new TestWebApplicationFactory();
            TestClient = TestWebAppFactory.CreateClient();
            TestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateTestToken());

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

        private static string GenerateTestToken()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "Admin"),
                new(ClaimTypes.Role, "Admin")
            };

            var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty));

            var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"] ?? string.Empty,
                audience: configuration["Jwt:Audience"] ?? string.Empty,
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(30),
                signingCredentials: credenciais
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
