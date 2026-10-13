using Dapper;
using GerenciamentoMecanicaSistema;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using NSubstitute;
using Service.Interface;
using Service.Interface.Events;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;

namespace ControllerTests;

public sealed class OrderOwnershipPersistenceTests
{
    private PostgreSqlContainer container = null!;
    private NpgsqlConnection connection = null!;
    private TestWebApplicationFactory sourceFactory = null!;
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;
    private readonly Guid customerId = Guid.NewGuid();
    private readonly Guid otherCustomerId = Guid.NewGuid();
    private readonly Guid vehicleId = Guid.NewGuid();
    private readonly Guid serviceId = Guid.NewGuid();
    private readonly Guid materialId = Guid.NewGuid();
    private const string Document = "662.119.730-63";
    private const string OtherDocument = "529.982.247-25";

    [OneTimeSetUp]
    public async Task SetUpDatabase()
    {
        container = new PostgreSqlBuilder("postgres:16").WithDatabase("ownership_tests")
            .WithUsername("postgres").WithPassword("local-tests-only").Build();
        await container.StartAsync();
        connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        // Minimal fixture for the existing repositories; no production SQL copied.
        await connection.ExecuteAsync("""
            CREATE TABLE customers (id UUID PRIMARY KEY, name TEXT NOT NULL, document TEXT UNIQUE NOT NULL,
                phone TEXT NOT NULL, email TEXT NOT NULL, status VARCHAR(8) NOT NULL DEFAULT 'Active');
            CREATE TABLE vehicles (id UUID PRIMARY KEY, customer_document TEXT REFERENCES customers(document),
                brand TEXT, model TEXT, year INTEGER, license_plate TEXT UNIQUE);
            CREATE TABLE catalog (id UUID PRIMARY KEY, description TEXT, hours REAL, price_per_hour NUMERIC);
            CREATE TABLE stock (id UUID PRIMARY KEY, name TEXT, brand TEXT, price NUMERIC,
                amount INTEGER, reserved_amount INTEGER DEFAULT 0);
            CREATE TABLE orders (id UUID PRIMARY KEY, customer_document TEXT REFERENCES customers(document),
                vehicle_license_plate TEXT REFERENCES vehicles(license_plate), budget NUMERIC, status TEXT);
            CREATE TABLE order_status_history (id UUID PRIMARY KEY, order_id UUID, sequence INTEGER,
                previous_status TEXT, new_status TEXT, occurred_at TIMESTAMPTZ, reason TEXT);
            CREATE TABLE order_services (id UUID REFERENCES catalog(id), order_id UUID REFERENCES orders(id),
                description TEXT, hours REAL, price_per_hour NUMERIC, amount INTEGER);
            CREATE TABLE order_materials (id UUID REFERENCES stock(id), order_id UUID REFERENCES orders(id),
                name TEXT, brand TEXT, price NUMERIC, amount INTEGER);
            """);
        sourceFactory = new TestWebApplicationFactory(container.GetConnectionString());
        factory = sourceFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOrdersService>();
            services.RemoveAll<IStockService>();
            services.RemoveAll<IApplicationEventDispatcher>();
            services.AddScoped<IOrdersService, Service.OrdersService>();
            services.AddScoped<IStockService, Service.StockService>();
            services.AddSingleton(Substitute.For<IApplicationEventDispatcher>());
        }));
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    [SetUp]
    public async Task ResetData()
    {
        await connection.ExecuteAsync("""
            TRUNCATE customers, vehicles, orders, order_status_history, order_services, order_materials, stock, catalog;
            INSERT INTO customers VALUES (@CustomerId, 'Owner', @Document, '(11) 91234-5678', 'owner@example.com', 'Active');
            INSERT INTO customers VALUES (@OtherId, 'Other', @OtherDocument, '(11) 91234-5678', 'other@example.com', 'Active');
            INSERT INTO vehicles VALUES (@VehicleId, @Document, 'Honda', 'Civic', 2025, 'TST1234');
            INSERT INTO catalog VALUES (@ServiceId, 'Service', 1, 20);
            INSERT INTO stock VALUES (@MaterialId, 'Material', 'Test', 10, 5, 0);
            """, new { CustomerId = customerId, Document, OtherId = otherCustomerId, OtherDocument, VehicleId = vehicleId, ServiceId = serviceId, MaterialId = materialId });
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

    private void UseToken(string role, Guid id) => client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create(role, id.ToString()));

    private async Task<Guid> CreateOrder()
    {
        UseToken("Mechanic", Guid.NewGuid());
        using var response = await client.PostAsJsonAsync("/Orders", new
        {
            customerId, vehicleId, services = new[] { new { id = serviceId, value = 1 } },
            materials = new[] { new { id = materialId, value = 1 } }
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private async Task CompleteDiagnosis(Guid id)
    {
        UseToken("Mechanic", Guid.NewGuid());
        using var start = await client.PatchAsync($"/Orders/{id}/diagnosis/start", null);
        using var complete = await client.PatchAsync($"/Orders/{id}/diagnosis/complete", null);
        Assert.That(start.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(complete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    private async Task<string> Snapshot()
    {
        return await connection.QuerySingleAsync<string>("""
            SELECT jsonb_build_object(
                'orders', (SELECT jsonb_agg(to_jsonb(o) ORDER BY id) FROM orders o),
                'history', (SELECT jsonb_agg(to_jsonb(h) ORDER BY order_id, sequence) FROM order_status_history h),
                'stock', (SELECT jsonb_agg(to_jsonb(s) ORDER BY id) FROM stock s))::text
            """);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task MechanicAndOwnerExerciseApprovedAndRejectedCycles(bool approved)
    {
        var id = await CreateOrder();
        await CompleteDiagnosis(id);
        UseToken("Customer", customerId);
        // Inactivation after issuance does not revoke this signed token.
        await connection.ExecuteAsync("UPDATE customers SET status = 'Inactive' WHERE id = @Id", new { Id = customerId });
        using var status = await client.GetAsync($"/Orders/{id}/status");
        Assert.That(status.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var decision = await client.PatchAsJsonAsync($"/Orders/{id}/budget", new { customerDocument = "66211973063", approved });
        Assert.That(decision.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        if (approved)
        {
            UseToken("Mechanic", Guid.NewGuid());
            foreach (var action in new[] { "execution/start", "execution/complete", "delivery" })
            {
                using var response = await client.PatchAsync($"/Orders/{id}/{action}", null);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            }
        }
        Assert.That(await connection.QuerySingleAsync<string>("SELECT status FROM orders WHERE id = @Id", new { Id = id }), Is.EqualTo(approved ? "Delivered" : "Finished"));
        Assert.That(await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM order_status_history WHERE order_id = @Id", new { Id = id }), Is.EqualTo(approved ? 7 : 4));
        Assert.That(await connection.QuerySingleAsync<string>("SELECT reason FROM order_status_history WHERE order_id = @Id AND new_status = 'Finished'", new { Id = id }), Is.EqualTo(approved ? "ServiceCompleted" : "BudgetRejected"));
        Assert.That(await connection.QuerySingleAsync<int>("SELECT reserved_amount FROM stock"), Is.Zero);
        Assert.That(await connection.QuerySingleAsync<int>("SELECT amount FROM stock"), Is.EqualTo(approved ? 4 : 5));
    }

    [TestCase("other")]
    [TestCase("missing-customer")]
    [TestCase("missing-order")]
    public async Task UnownedOrMissingResourceHasSame404AndNoWrites(string scenario)
    {
        var ownOrderId = await CreateOrder();
        await CompleteDiagnosis(ownOrderId);
        var id = scenario == "missing-order" ? Guid.NewGuid() : ownOrderId;
        var callerId = scenario switch
        {
            "other" => otherCustomerId,
            "missing-customer" => Guid.NewGuid(),
            _ => customerId
        };
        UseToken("Customer", callerId);
        var before = await Snapshot();
        using var read = await client.GetAsync($"/Orders/{id}/status");
        using var write = await client.PatchAsJsonAsync($"/Orders/{id}/budget", new { customerDocument = Document, approved = false });
        Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(write.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        using var readError = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        using var writeError = JsonDocument.Parse(await write.Content.ReadAsStringAsync());
        Assert.That(readError.RootElement.GetProperty("detail").GetString(), Is.EqualTo("Ordem não encontrada"));
        Assert.That(writeError.RootElement.GetProperty("detail").GetString(), Is.EqualTo("Ordem não encontrada"));
        Assert.That(await write.Content.ReadAsStringAsync(), Does.Not.Contain(Document).And.Not.Contain(OtherDocument));
        Assert.That(await Snapshot(), Is.EqualTo(before));
    }

    [Test]
    public async Task OwnerCannotDecideWithDivergentDocumentOrInvalidState()
    {
        var id = await CreateOrder();
        UseToken("Customer", customerId);
        var before = await Snapshot();
        using var state = await client.PatchAsJsonAsync($"/Orders/{id}/budget", new { customerDocument = Document, approved = true });
        Assert.That(state.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await Snapshot(), Is.EqualTo(before));
        await CompleteDiagnosis(id);
        UseToken("Customer", customerId);
        before = await Snapshot();
        using var document = await client.PatchAsJsonAsync($"/Orders/{id}/budget", new { customerDocument = OtherDocument, approved = false });
        Assert.That(document.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        using var invalid = await client.PatchAsJsonAsync($"/Orders/{id}/budget", new { customerDocument = "invalid", approved = false });
        Assert.That(invalid.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await Snapshot(), Is.EqualTo(before));
    }

    [TestCase("Admin")]
    [TestCase("Mechanic")]
    public async Task InternalProfilesCannotDecideBudgetEvenWithOwnersDocument(string role)
    {
        var id = await CreateOrder();
        await CompleteDiagnosis(id);
        UseToken(role, Guid.NewGuid());
        var before = await Snapshot();
        using var response = await client.PatchAsJsonAsync($"/Orders/{id}/budget", new { customerDocument = Document, approved = false });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(await Snapshot(), Is.EqualTo(before));
    }
}
