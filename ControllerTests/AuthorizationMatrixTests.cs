using NSubstitute;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Service.Interface.Commands.Order;

namespace ControllerTests;

public sealed class AuthorizationMatrixTests
{
    private TestWebApplicationFactory factory = null!;
    private HttpClient client = null!;
    private static readonly Guid IdentityId = Guid.NewGuid();
    private const string OrderId = "11111111-1111-1111-1111-111111111111";
    private static readonly (string Method, string Path, string Roles)[] Routes =
    [
        ("POST", "/Users", "Admin"), ("GET", "/Users", "Admin,Mechanic"),
        ("PATCH", "/Users", "Admin,Mechanic"),
        ("DELETE", "/Users?id=" + OrderId, "Admin"),
        ("POST", "/Customers", "Mechanic"), ("GET", "/Customers", "Mechanic"),
        ("PATCH", $"/Customers/{OrderId}", "Mechanic"), ("DELETE", $"/Customers/{OrderId}", "Mechanic"),
        ("POST", "/Vehicles", "Mechanic"), ("GET", "/Vehicles", "Mechanic"),
        ("PATCH", $"/Vehicles/{OrderId}", "Mechanic"), ("DELETE", $"/Vehicles/{OrderId}", "Mechanic"),
        ("POST", "/Catalog", "Mechanic"), ("GET", "/Catalog", "Mechanic"),
        ("PATCH", $"/Catalog/{OrderId}", "Mechanic"), ("DELETE", $"/Catalog/{OrderId}", "Mechanic"),
        ("POST", "/Stock", "Mechanic"), ("GET", "/Stock", "Mechanic"),
        ("POST", $"/Stock/amount/{OrderId}", "Mechanic"), ("PATCH", $"/Stock/amount/{OrderId}", "Mechanic"),
        ("PATCH", $"/Stock/price/{OrderId}", "Mechanic"), ("DELETE", $"/Stock/{OrderId}", "Mechanic"),
        ("POST", "/Orders", "Mechanic"), ("GET", $"/Orders/{OrderId}/status", "Mechanic,Customer"),
        ("GET", "/Orders/operational", "Mechanic"), ("GET", "/Orders/details", "Mechanic"),
        ("PATCH", $"/Orders/{OrderId}/diagnosis/start", "Mechanic"),
        ("POST", $"/Orders/{OrderId}/services", "Mechanic"), ("PATCH", $"/Orders/{OrderId}/services", "Mechanic"),
        ("POST", $"/Orders/{OrderId}/materials", "Mechanic"), ("PATCH", $"/Orders/{OrderId}/materials", "Mechanic"),
        ("PATCH", $"/Orders/{OrderId}/diagnosis/complete", "Mechanic"), ("PATCH", $"/Orders/{OrderId}/budget", "Customer"),
        ("PATCH", $"/Orders/{OrderId}/execution/start", "Mechanic"), ("PATCH", $"/Orders/{OrderId}/execution/complete", "Mechanic"),
        ("PATCH", $"/Orders/{OrderId}/delivery", "Mechanic"), ("DELETE", $"/Orders/{OrderId}", "Mechanic")
    ];

    public static IEnumerable<TestCaseData> MatrixCases()
    {
        foreach (var route in Routes)
            foreach (var role in new string?[] { null, "Admin", "Mechanic", "Customer", "User", "Other" })
                yield return new TestCaseData(route.Method, route.Path, role,
                    role is not null && route.Roles.Split(',').Contains(role));
    }

    [OneTimeSetUp]
    public void SetUp()
    {
        factory = new TestWebApplicationFactory();
        client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    [OneTimeTearDown]
    public void TearDown() { client.Dispose(); factory.Dispose(); }

    [TestCaseSource(nameof(MatrixCases))]
    public async Task EnforcesProfileForEveryExistingAction(string method, string path, string? role, bool allowed)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (role is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create(role, IdentityId.ToString()));
        if (method is "POST" or "PATCH") request.Content = JsonContent.Create(new { });
        using var response = await client.SendAsync(request);
        if (role is null) Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        else if (!allowed) Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        else
        {
            // Business validation is exercised separately; authorized requests must reach it.
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized).And.Not.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.InternalServerError));
        }
    }

    [TestCase("Mechanic", "/Users", null, false)]
    [TestCase("Mechanic", "/Users", "invalid", false)]
    [TestCase("Mechanic", "/Users", "00000000-0000-0000-0000-000000000000", false)]
    [TestCase("Mechanic", "/Users", OrderId, true)]
    [TestCase("Customer", "/Orders/" + OrderId + "/status", null, false)]
    [TestCase("Customer", "/Orders/" + OrderId + "/status", "invalid", false)]
    [TestCase("Customer", "/Orders/" + OrderId + "/status", "00000000-0000-0000-0000-000000000000", false)]
    [TestCase("Customer", "/Orders/" + OrderId + "/status", OrderId, true)]
    [TestCase("Customer", "/Orders/" + OrderId + "/budget", null, false)]
    [TestCase("Customer", "/Orders/" + OrderId + "/budget", "invalid", false)]
    [TestCase("Customer", "/Orders/" + OrderId + "/budget", "00000000-0000-0000-0000-000000000000", false)]
    [TestCase("Customer", "/Orders/" + OrderId + "/budget", OrderId, true)]
    public async Task InvalidRequiredIdentityIsChallengedBeforeService(string role, string path, string? id, bool duplicate)
    {
        factory.OrderServiceMock.ClearReceivedCalls(); factory.UserServiceMock.ClearReceivedCalls();
        using var request = new HttpRequestMessage(path.EndsWith("budget", StringComparison.Ordinal) ? HttpMethod.Patch : HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create(role, id, duplicate));
        if (request.Method == HttpMethod.Patch) request.Content = JsonContent.Create(new { customerDocument = "662.119.730-63", approved = true });
        using var response = await client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        await factory.OrderServiceMock.DidNotReceiveWithAnyArgs().GetCustomerOrderStatus(Guid.Empty, Guid.Empty);
        await factory.OrderServiceMock.DidNotReceiveWithAnyArgs().ApproveBudget(Guid.Empty, default!, Guid.Empty);
        await factory.UserServiceMock.DidNotReceiveWithAnyArgs().GetOwnUser(Guid.Empty, default!, default!);
    }

    [TestCase("Admin", "/Users")]
    [TestCase("Mechanic", "/Stock")]
    public async Task IdentityIsNotRequiredGlobally(string role, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create(role, null));
        using var response = await client.SendAsync(request);
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized).And.Not.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task BudgetIdentityComesFromJwtNotExtraBodyField()
    {
        factory.OrderServiceMock.ClearReceivedCalls();
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/Orders/{OrderId}/budget");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationTestTokens.Create("Customer", IdentityId.ToString()));
        request.Content = JsonContent.Create(new { customerDocument = "662.119.730-63", approved = true, customerId = Guid.NewGuid() });
        using var response = await client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await factory.OrderServiceMock.Received(1).ApproveBudget(Guid.Parse(OrderId), Arg.Any<ApproveOrderCommand>(), IdentityId);
    }
}
