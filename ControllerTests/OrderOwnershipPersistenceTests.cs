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
using ControllerTests.Observability;
using OpenTelemetry.Metrics;

namespace ControllerTests;

[NonParallelizable]
public sealed class OrderOwnershipPersistenceTests
{
    private PostgreSqlContainer container = null!;
    private NpgsqlConnection connection = null!;
    private TelemetryWebApplicationFactory sourceFactory = null!;
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
    }

    private void CreateApi()
    {
        sourceFactory = new TelemetryWebApplicationFactory(container.GetConnectionString())
        { TelemetryEnabled = "true", TelemetryEndpoint = "http://127.0.0.1:1" };
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

    private void FlushMetrics() => factory.Services.GetRequiredService<MeterProvider>().ForceFlush(5_000);

    [SetUp]
    public async Task ResetData()
    {
        await connection.ExecuteAsync("""
            DROP TRIGGER IF EXISTS fail_delete_test ON order_services;
            DROP TRIGGER IF EXISTS fail_delete_test ON order_materials;
            DROP TRIGGER IF EXISTS fail_delete_test ON orders;
            TRUNCATE customers, vehicles, orders, order_status_history, order_services, order_materials, stock, catalog;
            INSERT INTO customers VALUES (@CustomerId, 'Owner', @Document, '(11) 91234-5678', 'owner@example.com', 'Active');
            INSERT INTO customers VALUES (@OtherId, 'Other', @OtherDocument, '(11) 91234-5678', 'other@example.com', 'Active');
            INSERT INTO vehicles VALUES (@VehicleId, @Document, 'Honda', 'Civic', 2025, 'TST1234');
            INSERT INTO catalog VALUES (@ServiceId, 'Service', 1, 20);
            INSERT INTO stock VALUES (@MaterialId, 'Material', 'Test', 10, 5, 0);
            """, new { CustomerId = customerId, Document, OtherId = otherCustomerId, OtherDocument, VehicleId = vehicleId, ServiceId = serviceId, MaterialId = materialId });
        CreateApi();
    }

    [TearDown]
    public async Task TearDownApi()
    {
        client?.Dispose();
        if (factory is not null) await factory.DisposeAsync();
        if (sourceFactory is not null) await sourceFactory.DisposeAsync();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
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
        using (var details = await SendAuthorized(HttpMethod.Get, $"/Orders/details?id={id}"))
        using (var json = JsonDocument.Parse(await details.Content.ReadAsStringAsync()))
        {
            Assert.That(json.RootElement[0].GetProperty("dateFinished").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(json.RootElement[0].GetProperty("duration").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
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
        using var finalDetails = await SendAuthorized(HttpMethod.Get, $"/Orders/details?id={id}");
        using var finalJson = JsonDocument.Parse(await finalDetails.Content.ReadAsStringAsync());
        Assert.That(finalJson.RootElement[0].GetProperty("dateFinished").ValueKind,
            Is.EqualTo(approved ? JsonValueKind.String : JsonValueKind.Null));
        Assert.That(finalJson.RootElement[0].GetProperty("duration").ValueKind,
            Is.EqualTo(approved ? JsonValueKind.String : JsonValueKind.Null));
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
    private async Task<HttpResponseMessage> SendAuthorized(HttpMethod method, string path, object? body = null, string role = "Mechanic")
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create(role, customerId.ToString()));
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private object NewOrderBody(int amount = 1) => new
    {
        customerId, vehicleId, services = new[] { new { id = serviceId, value = 1 } },
        materials = new[] { new { id = materialId, value = amount } }
    };

    private async Task<HttpResponseMessage[]> RunContenders(string table, Guid id,
        Func<Task<HttpResponseMessage>> first, Func<Task<HttpResponseMessage>> second)
    {
        if (table is not "orders" and not "stock") throw new ArgumentException("Unexpected fixture table.", nameof(table));
        await using var blocker = new NpgsqlConnection(container.GetConnectionString());
        await blocker.OpenAsync();
        await using var transaction = await blocker.BeginTransactionAsync();
        await blocker.ExecuteAsync($"SELECT id FROM {table} WHERE id = @Id FOR UPDATE", new { Id = id }, transaction);
        var tasks = new[] { first(), second() };
        var blocked = false;
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            while (timeout.Elapsed < TimeSpan.FromSeconds(10))
            {
                var waiting = await connection.QuerySingleAsync<int>("""
                    SELECT count(*)::int FROM pg_stat_activity
                    WHERE datname = current_database() AND wait_event_type = 'Lock'
                    AND query ILIKE @Pattern
                    """, new { Pattern = "%" + table + "%" });
                if (waiting >= 2) { blocked = true; break; }
                await Task.Delay(25);
            }
        }
        finally
        {
            await transaction.RollbackAsync();
        }
        var responses = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(15));
        if (!blocked)
        {
            foreach (var response in responses) response.Dispose();
            Assert.Fail("Both operations must contend on the held database row before it is released.");
        }
        return responses;
    }

    private async Task AssertSingleDecision(HttpResponseMessage[] responses)
    {
        try
        {
            Assert.That(responses.Select(response => response.StatusCode),
                Is.EquivalentTo(new[] { HttpStatusCode.NoContent, HttpStatusCode.UnprocessableEntity }));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT reserved_amount FROM stock"), Is.EqualTo(1));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Test]
    public async Task ApprovalAndRejectionCannotOverwriteEachOther()
    {
        var id = await CreateOrder();
        await CompleteDiagnosis(id);
        var responses = await RunContenders("orders", id,
            () => SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/budget", new { customerDocument = Document, approved = true }, "Customer"),
            () => SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/budget", new { customerDocument = Document, approved = false }, "Customer"));
        try
        {
            Assert.That(responses.Select(response => response.StatusCode),
                Is.EquivalentTo(new[] { HttpStatusCode.NoContent, HttpStatusCode.UnprocessableEntity }));
            var status = await connection.QuerySingleAsync<string>("SELECT status FROM orders WHERE id = @Id", new { Id = id });
            Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_status_history WHERE order_id = @Id", new { Id = id }), Is.EqualTo(4));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT reserved_amount FROM stock"), Is.EqualTo(status == "Finished" ? 0 : 1));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT amount FROM stock"), Is.EqualTo(status == "Finished" ? 5 : 4));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RepeatedRejectionOrCompletionDoesNotUseAnotherOrdersReservation(bool complete)
    {
        var id = await CreateOrder();
        await CreateOrder(); // Independent reservation for the same SKU must survive.
        await CompleteDiagnosis(id);
        if (complete)
        {
            using var approve = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/budget", new { customerDocument = Document, approved = true }, "Customer");
            using var start = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/execution/start");
            Assert.That(start.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }
        var events = factory.Services.GetRequiredService<IApplicationEventDispatcher>();
        events.ClearReceivedCalls();
        var path = complete ? $"/Orders/{id}/execution/complete" : $"/Orders/{id}/budget";
        var body = complete ? null : (object)new { customerDocument = Document, approved = false };
        var role = complete ? "Mechanic" : "Customer";
        await AssertSingleDecision(await RunContenders("orders", id,
            () => SendAuthorized(HttpMethod.Patch, path, body, role), () => SendAuthorized(HttpMethod.Patch, path, body, role)));
        Assert.That(await connection.QuerySingleAsync<int>("SELECT amount FROM stock"), Is.EqualTo(complete ? 3 : 4));
        Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_status_history WHERE order_id = @Id", new { Id = id }), Is.EqualTo(complete ? 6 : 4));
        if (complete) await events.Received(1).Publish(Arg.Any<Service.Interface.Events.Order.OrderStatusChangedEvent>());
        else await events.DidNotReceive().Publish(Arg.Any<Service.Interface.Events.IApplicationEvent>());
        FlushMetrics();
        Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.created"), Is.EqualTo(2));
        Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.service.duration"), Is.EqualTo(complete ? 1 : 0));
    }

    [Test]
    public async Task ItemEditAndDiagnosisKeepBudgetAndReservationsConsistent()
    {
        var id = await CreateOrder();
        using var start = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/diagnosis/start");
        var responses = await RunContenders("orders", id,
            () => SendAuthorized(HttpMethod.Post, $"/Orders/{id}/materials", new { id = materialId, value = 1 }),
            () => SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/diagnosis/complete"));
        try
        {
            Assert.That(responses[0].StatusCode, Is.AnyOf(HttpStatusCode.OK, HttpStatusCode.UnprocessableEntity));
            Assert.That(responses[1].StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            var amount = await connection.QuerySingleAsync<int>("SELECT amount FROM order_materials WHERE order_id = @Id", new { Id = id });
            Assert.That(await connection.QuerySingleAsync<decimal>("SELECT budget FROM orders WHERE id = @Id", new { Id = id }), Is.EqualTo(20 + 10 * amount));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT reserved_amount FROM stock"), Is.EqualTo(amount));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT amount FROM stock"), Is.EqualTo(5 - amount));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_status_history WHERE order_id = @Id", new { Id = id }), Is.EqualTo(3));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [TestCase("services", true)]
    [TestCase("services", false)]
    [TestCase("materials", true)]
    [TestCase("materials", false)]
    public async Task ConcurrentItemAddsDoNotLoseAmountsOrCreateDuplicates(string kind, bool existing)
    {
        var id = await CreateOrder();
        using var start = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/diagnosis/start");
        var itemId = kind == "services" ? serviceId : materialId;
        if (!existing)
        {
            using var remove = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/{kind}", new { id = itemId, value = 1 });
            Assert.That(remove.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }
        var responses = await RunContenders("orders", id,
            () => SendAuthorized(HttpMethod.Post, $"/Orders/{id}/{kind}", new { id = itemId, value = 1 }),
            () => SendAuthorized(HttpMethod.Post, $"/Orders/{id}/{kind}", new { id = itemId, value = 1 }));
        try
        {
            Assert.That(responses.All(response => response.StatusCode == HttpStatusCode.OK), Is.True);
            var table = kind == "services" ? "order_services" : "order_materials";
            Assert.That(await connection.QuerySingleAsync<int>($"SELECT count(*) FROM {table} WHERE order_id = @Id", new { Id = id }), Is.EqualTo(1));
            Assert.That(await connection.QuerySingleAsync<int>($"SELECT amount FROM {table} WHERE order_id = @Id", new { Id = id }), Is.EqualTo(existing ? 3 : 2));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Test]
    public async Task TwoOrdersCannotReserveMoreThanAvailable()
    {
        var responses = await RunContenders("stock", materialId,
            () => SendAuthorized(HttpMethod.Post, "/Orders", NewOrderBody(4)),
            () => SendAuthorized(HttpMethod.Post, "/Orders", NewOrderBody(4)));
        try
        {
            Assert.That(responses.Select(response => response.StatusCode), Is.EquivalentTo(new[] { HttpStatusCode.Created, HttpStatusCode.UnprocessableEntity }));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM orders"), Is.EqualTo(1));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_status_history"), Is.EqualTo(1));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT amount FROM stock"), Is.EqualTo(1));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT reserved_amount FROM stock"), Is.EqualTo(4));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [TestCase("replenish", 6, 1)]
    [TestCase("remove", 3, 1)]
    [TestCase("consume", 4, 0)]
    [TestCase("reserve", 3, 2)]
    public async Task RejectionAndOtherStockOperationsDoNotLoseBalances(string operation, int available, int reserved)
    {
        var firstOrder = await CreateOrder();
        var secondOrder = await CreateOrder();
        await CompleteDiagnosis(firstOrder);
        if (operation == "consume")
        {
            await CompleteDiagnosis(secondOrder);
            using var approve = await SendAuthorized(HttpMethod.Patch, $"/Orders/{secondOrder}/budget", new { customerDocument = Document, approved = true }, "Customer");
            using var start = await SendAuthorized(HttpMethod.Patch, $"/Orders/{secondOrder}/execution/start");
        }
        var responses = await RunContenders("stock", materialId,
            () => SendAuthorized(HttpMethod.Patch, $"/Orders/{firstOrder}/budget", new { customerDocument = Document, approved = false }, "Customer"),
            () => operation switch
            {
                "replenish" => SendAuthorized(HttpMethod.Post, $"/Stock/amount/{materialId}", new { value = 2 }),
                "remove" => SendAuthorized(HttpMethod.Patch, $"/Stock/amount/{materialId}", new { value = 1 }),
                "consume" => SendAuthorized(HttpMethod.Patch, $"/Orders/{secondOrder}/execution/complete"),
                _ => SendAuthorized(HttpMethod.Post, "/Orders", NewOrderBody())
            });
        try
        {
            Assert.That(responses[0].StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            var expected = operation switch
            {
                "replenish" => HttpStatusCode.OK,
                "reserve" => HttpStatusCode.Created,
                _ => HttpStatusCode.NoContent
            };
            Assert.That(responses[1].StatusCode, Is.EqualTo(expected));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT amount FROM stock"), Is.EqualTo(available));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT reserved_amount FROM stock"), Is.EqualTo(reserved));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Test]
    public async Task DeletionAndCompletionUseTheCurrentStateBeforeChangingStock()
    {
        var id = await CreateOrder();
        await CreateOrder();
        await CompleteDiagnosis(id);
        using var approve = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/budget", new { customerDocument = Document, approved = true }, "Customer");
        using var start = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/execution/start");
        var responses = await RunContenders("orders", id,
            () => SendAuthorized(HttpMethod.Delete, $"/Orders/{id}"),
            () => SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/execution/complete"));
        try
        {
            Assert.That(responses[0].StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(responses[1].StatusCode, Is.AnyOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound));
            var completed = responses[1].StatusCode == HttpStatusCode.NoContent;
            Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM orders WHERE id = @Id", new { Id = id }), Is.Zero);
            Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_status_history WHERE order_id = @Id", new { Id = id }), Is.EqualTo(completed ? 6 : 5));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT amount FROM stock"), Is.EqualTo(completed ? 3 : 4));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT reserved_amount FROM stock"), Is.EqualTo(1));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Test]
    public async Task MultipleMaterialsAreLockedInTheSameOrderForDifferentRequests()
    {
        var otherMaterial = Guid.NewGuid();
        await connection.ExecuteAsync("INSERT INTO stock VALUES (@Id, 'Second material', 'Test', 10, 5, 0)", new { Id = otherMaterial });
        object Body(Guid first, Guid second) => new
        {
            customerId, vehicleId, services = new[] { new { id = serviceId, value = 1 } },
            materials = new[] { new { id = first, value = 1 }, new { id = second, value = 1 } }
        };
        var firstLock = new[] { materialId, otherMaterial }.Min();
        var responses = await RunContenders("stock", firstLock,
            () => SendAuthorized(HttpMethod.Post, "/Orders", Body(materialId, otherMaterial)),
            () => SendAuthorized(HttpMethod.Post, "/Orders", Body(otherMaterial, materialId)));
        try
        {
            Assert.That(responses.All(response => response.StatusCode == HttpStatusCode.Created), Is.True);
            Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM stock WHERE amount = 3 AND reserved_amount = 2"), Is.EqualTo(2));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_status_history"), Is.EqualTo(2));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [TestCase("orders", "Received")]
    [TestCase("order_materials", "Received")]
    [TestCase("order_services", "Received")]
    [TestCase("orders", "Finished")]
    [TestCase("order_materials", "Finished")]
    [TestCase("order_services", "Finished")]
    [TestCase("orders", "Delivered")]
    [TestCase("order_materials", "Delivered")]
    [TestCase("order_services", "Delivered")]
    public async Task FailedDeletionRollsBackEveryStepAndPreservesHistory(string table, string status)
    {
        var id = await CreateOrder();
        if (status != "Received")
        {
            await CompleteDiagnosis(id);
            using var decision = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/budget",
                new { customerDocument = Document, approved = status == "Delivered" }, "Customer");
            if (status == "Delivered")
                foreach (var action in new[] { "execution/start", "execution/complete", "delivery" })
                {
                    using var transition = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/{action}");
                    Assert.That(transition.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                }
        }
        if (table is not "orders" and not "order_materials" and not "order_services") throw new ArgumentException("Unexpected fixture table.", nameof(table));
        await connection.ExecuteAsync($"""
            CREATE OR REPLACE FUNCTION reject_test_deletion() RETURNS trigger AS $$
            BEGIN RAISE EXCEPTION 'Controlled local deletion failure'; END;
            $$ LANGUAGE plpgsql;
            CREATE TRIGGER fail_delete_test BEFORE DELETE ON {table}
            FOR EACH ROW EXECUTE FUNCTION reject_test_deletion();
            """);
        var before = await Snapshot();
        var servicesBefore = await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_services");
        var materialsBefore = await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_materials");
        using var response = await SendAuthorized(HttpMethod.Delete, $"/Orders/{id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(await Snapshot(), Is.EqualTo(before));
        Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_services"), Is.EqualTo(servicesBefore));
        Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_materials"), Is.EqualTo(materialsBefore));
        FlushMetrics();
        Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.processing.failures", ("operation", "DeleteOrder")), Is.EqualTo(1));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task BusinessMetricsMatchRetainedHistoryAfterAnApprovedOrRejectedOrderIsDeleted(bool approved)
    {
        var id = await CreateOrder();
        var opening = DateTime.UtcNow.AddDays(-7);
        await connection.ExecuteAsync("UPDATE order_status_history SET occurred_at = @Opening WHERE order_id = @Id", new { Opening = opening, Id = id });
        using var startDiagnosis = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/diagnosis/start");
        await connection.ExecuteAsync("UPDATE order_status_history SET occurred_at = @Started WHERE order_id = @Id AND sequence = 2",
            new { Started = opening.AddDays(1), Id = id });
        using var diagnosis = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/diagnosis/complete");
        using var decision = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/budget", new { customerDocument = Document, approved }, "Customer");
        if (approved)
            foreach (var action in new[] { "execution/start", "execution/complete" })
            {
                using var response = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/{action}");
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            }
        using var delivery = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/delivery");
        Assert.That(delivery.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        using var deletion = await SendAuthorized(HttpMethod.Delete, $"/Orders/{id}");
        Assert.That(deletion.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var expected = await connection.QuerySingleAsync<double>("""
            WITH intervals AS (
                SELECT *, lag(occurred_at) OVER (ORDER BY sequence) AS entered_at
                FROM order_status_history WHERE order_id = @Id
            )
            SELECT sum(extract(epoch FROM occurred_at - entered_at))::double precision
            FROM intervals
            WHERE new_status IN ('WaitingForApproval', 'Delivered')
                OR (new_status = 'Finished' AND reason = 'ServiceCompleted');
            """, new { Id = id });
        FlushMetrics();
        Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.created"), Is.EqualTo(1));
        Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.stage.duration"), Is.EqualTo(approved ? 3 : 2));
        Assert.That(BusinessMetricAssertions.Sum(sourceFactory.Metrics, "mecanica.orders.stage.duration"), Is.EqualTo(expected).Within(0.00001));
        Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.service.duration"), Is.EqualTo(approved ? 1 : 0));
        Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.stage.duration",
            ("completion.reason", approved ? "service_completed" : "budget_rejected")), Is.EqualTo(1));
        Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM orders"), Is.Zero);
        Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_status_history WHERE sequence = 1"), Is.EqualTo(1));
        if (approved)
        {
            var total = await connection.QuerySingleAsync<double>("""
                SELECT extract(epoch FROM max(occurred_at) FILTER (WHERE reason = 'ServiceCompleted')
                    - min(occurred_at) FILTER (WHERE sequence = 1))::double precision
                FROM order_status_history WHERE order_id = @Id;
                """, new { Id = id });
            Assert.That(BusinessMetricAssertions.Sum(sourceFactory.Metrics, "mecanica.orders.service.duration"), Is.EqualTo(total).Within(0.00001));
        }
    }

    [Test]
    public async Task FailedCreationRollsBackWithoutRecordingVolume()
    {
        await connection.ExecuteAsync("""
            CREATE OR REPLACE FUNCTION reject_test_creation() RETURNS trigger AS $$
            BEGIN RAISE EXCEPTION 'private-creation-error'; END;
            $$ LANGUAGE plpgsql;
            CREATE TRIGGER fail_create_test BEFORE INSERT ON order_services
            FOR EACH ROW EXECUTE FUNCTION reject_test_creation();
            """);
        try
        {
            using var response = await SendAuthorized(HttpMethod.Post, "/Orders", NewOrderBody());
            FlushMetrics();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.created"), Is.Zero);
            Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.processing.failures", ("operation", "CreateOrder")), Is.EqualTo(1));
            Assert.That(await connection.QuerySingleAsync<int>("SELECT count(*) FROM order_status_history"), Is.Zero);
            Assert.That(await connection.QuerySingleAsync<int>("SELECT reserved_amount FROM stock"), Is.Zero);
        }
        finally { await connection.ExecuteAsync("DROP TRIGGER fail_create_test ON order_services;"); }
    }

    [Test]
    public async Task ApiRestartUsesPersistedStageStartWithoutReplayingCreation()
    {
        var id = await CreateOrder();
        using var start = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/diagnosis/start");
        var opening = new DateTime(2026, 9, 30, 2, 59, 0, DateTimeKind.Utc);
        await connection.ExecuteAsync("UPDATE order_status_history SET occurred_at = @Opening + (sequence - 1) * interval '1 day' WHERE order_id = @Id",
            new { Opening = opening, Id = id });
        client.Dispose();
        await factory.DisposeAsync();
        await sourceFactory.DisposeAsync();
        CreateApi();
        FlushMetrics();
        Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.created"), Is.Zero);
        using var completion = await SendAuthorized(HttpMethod.Patch, $"/Orders/{id}/diagnosis/complete");
        Assert.That(completion.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var expected = await connection.QuerySingleAsync<double>("""
            SELECT extract(epoch FROM max(occurred_at) FILTER (WHERE sequence = 3)
                - max(occurred_at) FILTER (WHERE sequence = 2))::double precision
            FROM order_status_history WHERE order_id = @Id;
            """, new { Id = id });
        FlushMetrics();
        Assert.That(BusinessMetricAssertions.Sum(sourceFactory.Metrics, "mecanica.orders.stage.duration"), Is.EqualTo(expected).Within(0.00001));
        Assert.That(BusinessMetricAssertions.Count(sourceFactory.Metrics, "mecanica.orders.created"), Is.Zero);
    }

}
