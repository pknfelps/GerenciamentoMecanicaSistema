using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.PostgreSql;

namespace ControllerTests
{
    public class HealthControllerTests : BaseControllerTests
    {
        private PostgreSqlContainer PostgresContainer { get; set; }

        public override async Task SetUp()
        {
            PostgresContainer = new PostgreSqlBuilder("postgres:16")
                .WithDatabase("postgres")
                .WithUsername("postgres")
                .WithPassword("adm123")
                .Build();

            await PostgresContainer.StartAsync();

            TestWebAppFactory = new TestWebApplicationFactory(PostgresContainer.GetConnectionString());
            TestClient = TestWebAppFactory.CreateClient();
        }

        protected override void MockService()
        {
            // Ignored
        }

        [TearDown]
        public override async Task TearDown()
        {
            await base.TearDown();
            await PostgresContainer.DisposeAsync();
        }

        [TestCase("live")]
        [TestCase("ready")]
        [TestCase("startup")]
        public async Task MustReturnOkWhenHealthCheckIsHealthy(string endpoint)
        {
            var response = await TestClient.GetAsync($"/health/{endpoint}");
            
            var content = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(content, Is.EqualTo(nameof(HealthStatus.Healthy)));
            });
        }

        [Test]
        public async Task MustReturnServiceUnavailableWhenReadinessIsUnhealthy()
        {
            await PostgresContainer.StopAsync();
            
            var response = await TestClient.GetAsync($"/health/ready");
            
            var content = await response.Content.ReadAsStringAsync();
            
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
                Assert.That(content, Is.EqualTo(nameof(HealthStatus.Unhealthy)));
            });
        }
    }
}
