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

    private async Task<string> LoginToken(string name, string password, string role)
    {
        using var login = await client.PostAsJsonAsync("/Authentication", new { name, password, role });
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await login.Content.ReadAsStringAsync();
        return body.StartsWith('"') ? JsonSerializer.Deserialize<string>(body)! : body;
    }

    private async Task<Guid> RegisterAccount(string role)
    {
        using var response = await client.PostAsJsonAsync("/Users", new { name = "Original user", password = "Original@Test123", role });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        return await connection.QuerySingleAsync<Guid>("SELECT id FROM users WHERE name = 'Original user' AND role = @Role", new { Role = role });
    }

    [TestCase("Admin", "name")]
    [TestCase("Admin", "password")]
    [TestCase("Admin", "both")]
    [TestCase("Mechanic", "name")]
    [TestCase("Mechanic", "password")]
    [TestCase("Mechanic", "both")]
    public async Task OwnPatchPreservesIdentityAndUpdatesActualLogin(string role, string fields)
    {
        var id = await RegisterAccount(role);
        var oldHash = await connection.QuerySingleAsync<string>("SELECT password FROM users WHERE id = @Id", new { Id = id });
        var originalToken = await LoginToken("Original user", "Original@Test123", role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", originalToken);
        var body = new Dictionary<string, string>();
        if (fields != "password") body["name"] = "Changed user";
        if (fields != "name") body["password"] = "Changed@Test123";
        using var response = await client.PatchAsJsonAsync("/Users", body);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
        var stored = await connection.QuerySingleAsync<(Guid Id, string Name, string Hash, string Role)>(
            "SELECT id, name, password, role FROM users WHERE id = @Id", new { Id = id });
        Assert.That(stored.Id, Is.EqualTo(id));
        Assert.That(stored.Role, Is.EqualTo(role));
        Assert.That(stored.Name, Is.EqualTo(fields == "password" ? "Original user" : "Changed user"));
        var expectedPassword = fields == "name" ? "Original@Test123" : "Changed@Test123";
        Assert.That(new Pbkdf2PasswordHasher().Verify(expectedPassword, stored.Hash), Is.True);
        Assert.That(stored.Hash, Is.Not.EqualTo(expectedPassword));
        if (fields == "name") Assert.That(stored.Hash, Is.EqualTo(oldHash));
        else Assert.That(stored.Hash, Is.Not.EqualTo(oldHash));
        await LoginToken(stored.Name, expectedPassword, role);
        using var oldLogin = await client.PostAsJsonAsync("/Authentication", new { name = "Original user", password = "Original@Test123", role });
        Assert.That(oldLogin.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        // The original JWT remains valid and targets this UUID after the update.
        using var repeat = await client.PatchAsJsonAsync("/Users", new { name = stored.Name });
        Assert.That(repeat.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task ReusingOldLoginDoesNotTransferTheOriginalToken()
    {
        var id = await RegisterAccount("Mechanic");
        var token = await LoginToken("Original user", "Original@Test123", "Mechanic");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var rename = await client.PatchAsJsonAsync("/Users", new { name = "Changed user" });
        Assert.That(rename.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var otherId = Guid.NewGuid();
        var otherHash = new Pbkdf2PasswordHasher().Hash("Other@Test123");
        await connection.ExecuteAsync("INSERT INTO users VALUES (@Id, 'Original user', @Hash, 'Mechanic')", new { Id = otherId, Hash = otherHash });
        using var self = JsonDocument.Parse(await client.GetStringAsync("/Users"));
        Assert.That(self.RootElement.GetProperty("id").GetGuid(), Is.EqualTo(id));
        using var update = await client.PatchAsJsonAsync($"/Users?id={otherId}", new { name = "Changed again", password = "Changed@Test123" });
        Assert.That(update.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await connection.QuerySingleAsync<string>("SELECT name FROM users WHERE id = @Id", new { Id = id }), Is.EqualTo("Changed again"));
        Assert.That(await connection.QuerySingleAsync<string>("SELECT name FROM users WHERE id = @Id", new { Id = otherId }), Is.EqualTo("Original user"));
        Assert.That(await connection.QuerySingleAsync<string>("SELECT password FROM users WHERE id = @Id", new { Id = otherId }), Is.EqualTo(otherHash));
    }

    [TestCase("{}", HttpStatusCode.BadRequest)]
    [TestCase("{\"name\":null}", HttpStatusCode.BadRequest)]
    [TestCase("{\"password\":\"invalid\"}", HttpStatusCode.BadRequest)]
    [TestCase("{\"name\":\"Changed user\",\"role\":\"Admin\"}", HttpStatusCode.BadRequest)]
    [TestCase("{\"name\":\"Changed user\",\"id\":\"11111111-1111-1111-1111-111111111111\"}", HttpStatusCode.BadRequest)]
    [TestCase("{\"name\":\"Other user\"}", HttpStatusCode.Conflict)]
    public async Task InvalidOrConflictingPatchDoesNotChangeAnyAccount(string body, HttpStatusCode expected)
    {
        await RegisterAccount("Mechanic");
        await connection.ExecuteAsync("INSERT INTO users VALUES (@Id, 'Other user', @Hash, 'Mechanic')", new { Id = Guid.NewGuid(), Hash = "local-hash-placeholder" });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginToken("Original user", "Original@Test123", "Mechanic"));
        var before = await connection.QuerySingleAsync<string>("SELECT jsonb_agg(to_jsonb(u) ORDER BY id)::text FROM users u");
        using var response = await client.PatchAsync("/Users", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        Assert.That(response.StatusCode, Is.EqualTo(expected));
        Assert.That(await connection.QuerySingleAsync<string>("SELECT jsonb_agg(to_jsonb(u) ORDER BY id)::text FROM users u"), Is.EqualTo(before));
        var error = await response.Content.ReadAsStringAsync();
        Assert.That(error, Does.Not.Contain("local-hash-placeholder").And.Not.Contain("Original@Test123"));
    }

    [Test]
    public async Task SameNameOnOtherProfileIsAllowedAndSameOwnNameIsNotAConflict()
    {
        var id = await RegisterAccount("Mechanic");
        await connection.ExecuteAsync("INSERT INTO users VALUES (@Id, 'Other user', @Hash, 'Admin')", new { Id = Guid.NewGuid(), Hash = "local-hash-placeholder" });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginToken("Original user", "Original@Test123", "Mechanic"));
        using var changed = await client.PatchAsJsonAsync("/Users", new { name = "Other user" });
        Assert.That(changed.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        using var unchanged = await client.PatchAsJsonAsync("/Users", new { name = "Other user" });
        Assert.That(unchanged.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await connection.QuerySingleAsync<string>("SELECT role FROM users WHERE id = @Id", new { Id = id }), Is.EqualTo("Mechanic"));
    }

    [Test]
    public async Task PatchForMissingAccountReturns404()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create("Admin", Guid.NewGuid().ToString()));
        using var response = await client.PatchAsJsonAsync("/Users", new { name = "Changed user" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM users"), Is.Zero);
    }
}
