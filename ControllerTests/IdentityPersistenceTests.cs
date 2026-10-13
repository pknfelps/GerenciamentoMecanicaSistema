using Dapper;
using GerenciamentoMecanica.Auth.Contracts;
using GerenciamentoMecanicaSistema;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using Service.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Testcontainers.PostgreSql;

namespace ControllerTests;

public sealed class IdentityPersistenceTests
{
    private PostgreSqlContainer container = null!;
    private NpgsqlConnection connection = null!;
    private TestWebApplicationFactory sourceFactory = null!;
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;

    [OneTimeSetUp]
    public async Task SetUpDatabase()
    {
        container = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("identity_tests").WithUsername("postgres").WithPassword("local-tests-only").Build();
        await container.StartAsync();
        connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await connection.ExecuteAsync("""
            CREATE TABLE customers (
                id UUID PRIMARY KEY, name VARCHAR(100) NOT NULL, document VARCHAR(100) NOT NULL UNIQUE,
                phone VARCHAR(100) NOT NULL, email VARCHAR(100) NOT NULL,
                status VARCHAR(8) NOT NULL DEFAULT 'Active');
            CREATE TABLE users (
                id UUID PRIMARY KEY, name VARCHAR(100) NOT NULL, password VARCHAR(100) NOT NULL,
                role VARCHAR(100) NOT NULL);
            """);
        sourceFactory = new TestWebApplicationFactory(container.GetConnectionString());
        factory = sourceFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICustomerService>();
            services.RemoveAll<IUserService>();
            services.RemoveAll<IAuthenticationService>();
            services.AddScoped<ICustomerService, Service.CustomerService>();
            services.AddScoped<IUserService, Service.UserService>();
            services.AddScoped<IAuthenticationService, Service.AuthenticationService>();
        }));
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            new JwtIssuer(TestWebApplicationFactory.JwtKey, TestWebApplicationFactory.JwtIssuer, TestWebApplicationFactory.JwtAudience)
                .Generate("test admin", "Admin", Guid.NewGuid()));
    }

    [SetUp]
    public async Task ResetData()
    {
        await connection.ExecuteAsync("TRUNCATE customers, users");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            AuthorizationTestTokens.Create("Admin", Guid.NewGuid().ToString()));
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        client?.Dispose();
        if (factory is not null) await factory.DisposeAsync();
        if (sourceFactory is not null) await sourceFactory.DisposeAsync();
        if (connection is not null) await connection.DisposeAsync();
        if (container is not null) await container.DisposeAsync();
    }

    [Test]
    public async Task ExtraStatusCannotCreateInactiveCustomer()
    {
        UseMechanic();
        using var response = await client.PostAsJsonAsync("/Customers", new
        {
            name = "Test customer", document = "66211973063", phone = "11912345678",
            email = "customer@example.com", status = "Inactive"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(await connection.QuerySingleAsync<string>("SELECT status FROM customers"), Is.EqualTo("Active"));
        using var customers = JsonDocument.Parse(await client.GetStringAsync("/Customers"));
        Assert.That(customers.RootElement[0].GetProperty("status").GetString(), Is.EqualTo("Active"));
    }

    [Test]
    public async Task ExtraStatusCannotReactivateCustomerOnPatch()
    {
        UseMechanic();
        var id = Guid.NewGuid();
        await connection.ExecuteAsync("""
            INSERT INTO customers VALUES (@Id, 'Test customer', '662.119.730-63', '(11) 91234-5678', 'customer@example.com', 'Inactive')
            """, new { Id = id });
        using var response = await client.PatchAsJsonAsync($"/Customers/{id}", new
        {
            name = "Changed customer", document = "66211973063", phone = "11943218765",
            email = "changed@example.com", status = "Active"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        using var customers = JsonDocument.Parse(await client.GetStringAsync($"/Customers?id={id}"));
        Assert.That(customers.RootElement[0].GetProperty("status").GetString(), Is.EqualTo("Inactive"));
        Assert.That(customers.RootElement[0].GetProperty("name").GetString(), Is.EqualTo("Changed customer"));
    }

    [TestCase("Admin")]
    [TestCase("Mechanic")]
    public async Task RegisteredUserCanLoginWithStoredIdentity(string role)
    {
        var forgedId = Guid.NewGuid();
        using var registration = await client.PostAsJsonAsync("/Users", new { name = "Test user", password = "Safe@Test123", role, id = forgedId });
        Assert.That(registration.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var storedId = await connection.QuerySingleAsync<Guid>("SELECT id FROM users");
        var before = DateTime.UtcNow;
        using var login = await client.PostAsJsonAsync("/Authentication", new { name = "Test user", password = "Safe@Test123", role, id = forgedId });
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var raw = await login.Content.ReadAsStringAsync();
        var token = raw.StartsWith('"') ? JsonSerializer.Deserialize<string>(raw)! : raw;
        var options = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        var validation = await options.TokenHandlers[0].ValidateTokenAsync(token, options.TokenValidationParameters);
        Assert.That(validation.IsValid, Is.True);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Multiple(() =>
        {
            Assert.That(validation.ClaimsIdentity.Name, Is.EqualTo("Test user"));
            Assert.That(validation.ClaimsIdentity.FindFirst(ClaimTypes.Role)!.Value, Is.EqualTo(role));
            Assert.That(validation.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)!.Value, Is.EqualTo(storedId.ToString()));
            Assert.That(storedId, Is.Not.EqualTo(forgedId));
            Assert.That(jwt.Header.Alg, Is.EqualTo("HS256"));
            Assert.That(jwt.Issuer, Is.EqualTo(TestWebApplicationFactory.JwtIssuer));
            Assert.That(jwt.Audiences, Does.Contain(TestWebApplicationFactory.JwtAudience));
            Assert.That(jwt.ValidTo, Is.InRange(before.AddMinutes(10).AddSeconds(-1), DateTime.UtcNow.AddMinutes(10)));
        });
        using var wrongPassword = await client.PostAsJsonAsync("/Authentication", new { name = "Test user", password = "Wrong@Test123", role });
        Assert.That(wrongPassword.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase("User")]
    [TestCase("Manager")]
    [TestCase("Customer")]
    [TestCase("0")]
    [TestCase("Admin, Mechanic")]
    public async Task InvalidRoleIsRejectedWithoutRegisteringUser(string role)
    {
        using var registration = await client.PostAsJsonAsync("/Users", new { name = "Test user", password = "Safe@Test123", role });
        using var login = await client.PostAsJsonAsync("/Authentication", new { name = "Test user", password = "Safe@Test123", role });
        Assert.That(registration.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users"), Is.Zero);
    }

    [Test]
    public async Task InvalidStoredUserDoesNotProduceTokenOrCredentialDetails()
    {
        var hash = new Pbkdf2PasswordHasher().Hash("Safe@Test123");
        await connection.ExecuteAsync("INSERT INTO users VALUES (@Id, 'Test user', @Hash, 'Admin')", new { Id = Guid.Empty, Hash = hash });
        using var login = await client.PostAsJsonAsync("/Authentication", new { name = "Test user", password = "Safe@Test123", role = "Admin" });
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var error = await login.Content.ReadAsStringAsync();
        Assert.That(error, Does.Not.Contain(hash).And.Not.Contain("Safe@Test123"));
    }

    [Test]
    public async Task InvalidStoredCustomerStatusIsTechnicalFailure()
    {
        UseMechanic();
        await connection.ExecuteAsync("""
            INSERT INTO customers VALUES (@Id, 'Test customer', '662.119.730-63', '(11) 91234-5678', 'customer@example.com', 'Unknown')
            """, new { Id = Guid.NewGuid() });
        using var response = await client.GetAsync("/Customers");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    private void UseMechanic() => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
        new JwtIssuer(TestWebApplicationFactory.JwtKey, TestWebApplicationFactory.JwtIssuer, TestWebApplicationFactory.JwtAudience)
            .Generate("test mechanic", "Mechanic", Guid.NewGuid()));

    [Test]
    public async Task MechanicReadsOnlyCurrentAccountByIdAndAdminCanQueryOtherAccounts()
    {
        var ownId = Guid.NewGuid(); var otherId = Guid.NewGuid();
        await connection.ExecuteAsync("""
            INSERT INTO users VALUES (@OwnId, 'Current mechanic', 'local-hash-placeholder', 'Mechanic');
            INSERT INTO users VALUES (@OtherId, 'Other mechanic', 'local-hash-placeholder', 'Mechanic');
            """, new { OwnId = ownId, OtherId = otherId });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create("Mechanic", ownId.ToString(), name: "Previous name"));
        using var own = await client.GetAsync("/Users");
        Assert.That(own.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await own.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.That(json.RootElement.GetProperty("id").GetGuid(), Is.EqualTo(ownId));
        Assert.That(json.RootElement.GetProperty("name").GetString(), Is.EqualTo("Current mechanic"));
        Assert.That(body, Does.Not.Contain("password").And.Not.Contain("hash"));
        using var filtered = await client.GetAsync("/Users?name=Current%20mechanic&role=Mechanic");
        Assert.That(filtered.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var forbidden = await client.GetAsync("/Users?name=Other%20mechanic&role=Mechanic");
        Assert.That(forbidden.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(await forbidden.Content.ReadAsStringAsync(), Does.Not.Contain(otherId.ToString()));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create("Admin", null));
        using var admin = await client.GetAsync("/Users?name=Other%20mechanic&role=Mechanic");
        Assert.That(admin.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task MissingOwnAccountIsNotFoundWithoutRevokingToken()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create("Mechanic", Guid.NewGuid().ToString()));
        using var response = await client.GetAsync("/Users");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
