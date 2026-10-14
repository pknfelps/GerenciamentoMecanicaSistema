using GerenciamentoMecanicaSistema;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;

namespace ControllerTests;

public sealed class OpenApiContractTests
{
    private TestWebApplicationFactory sourceFactory = null!;
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;
    private JsonDocument document = null!;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        sourceFactory = new TestWebApplicationFactory();
        factory = sourceFactory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        document?.Dispose();
        client?.Dispose();
        factory?.Dispose();
        sourceFactory?.Dispose();
    }

    [Test]
    public void OrderCompletionAndDurationAreNullable()
    {
        foreach (var name in new[] { "WorkOrderResponse", "DetailedWorkOrderResponse" })
        {
            var properties = document.RootElement.GetProperty("components").GetProperty("schemas")
                .GetProperty(name).GetProperty("properties");
            Assert.That(properties.GetProperty("dateFinished").GetProperty("nullable").GetBoolean(), Is.True);
            Assert.That(properties.GetProperty("duration").GetProperty("nullable").GetBoolean(), Is.True);
            Assert.That(properties.GetProperty("dateCreated").TryGetProperty("nullable", out var openingNullable)
                && openingNullable.GetBoolean(), Is.False);
        }
    }

    [Test]
    public void DescribesVersionedOpenApi30ContractWithoutGlobalAuthentication()
    {
        var root = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("openapi").GetString(), Does.StartWith("3.0."));
            Assert.That(root.GetProperty("info").GetProperty("title").GetString(), Is.EqualTo("API da oficina"));
            Assert.That(root.GetProperty("info").GetProperty("version").GetString(), Is.EqualTo("1.0.0"));
            Assert.That(!root.TryGetProperty("security", out var security) || security.GetArrayLength() == 0, Is.True);
            Assert.That(!root.TryGetProperty("servers", out var servers) || servers.GetArrayLength() == 0, Is.True);
        });
    }

    [Test]
    public void DeclaresHttpBearerJwt()
    {
        var scheme = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        Assert.Multiple(() =>
        {
            Assert.That(scheme.GetProperty("type").GetString(), Is.EqualTo("http"));
            Assert.That(scheme.GetProperty("scheme").GetString(), Is.EqualTo("bearer"));
            Assert.That(scheme.GetProperty("bearerFormat").GetString(), Is.EqualTo("JWT"));
        });
    }

    [TestCase("/Customers", "post")]
    [TestCase("/Users", "get")]
    [TestCase("/Vehicles", "post")]
    [TestCase("/Catalog", "get")]
    [TestCase("/Stock", "get")]
    [TestCase("/Orders", "post")]
    [TestCase("/Orders/{id}/status", "get")]
    [TestCase("/Orders/{id}/budget", "patch")]
    public void ProtectedOperationsRequireBearerInheritedFromController(string path, string method)
    {
        var security = Operation(path, method).GetProperty("security");

        Assert.That(security.GetArrayLength(), Is.EqualTo(1));
        Assert.That(security[0].GetProperty("Bearer").GetArrayLength(), Is.Zero);
    }

    [TestCase("/Authentication", "post")]
    [TestCase("/health/live", "get")]
    [TestCase("/health/ready", "get")]
    [TestCase("/health/startup", "get")]
    public void PublicOperationsDoNotRequireBearer(string path, string method)
    {
        var operation = Operation(path, method);

        Assert.That(!operation.TryGetProperty("security", out var security) || security.GetArrayLength() == 0, Is.True);
    }

    [Test]
    public void PreservesAuthenticationBodyAndResponsesAndOrderRouteParameter()
    {
        var authentication = Operation("/Authentication", "post");
        var request = authentication.GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");
        var schemaName = request.GetProperty("$ref").GetString()!.Split('/')[^1];
        var properties = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty(schemaName).GetProperty("properties");
        var orderId = Operation("/Orders/{id}/status", "get").GetProperty("parameters")[0];

        Assert.Multiple(() =>
        {
            Assert.That(properties.TryGetProperty("name", out _), Is.True);
            Assert.That(properties.TryGetProperty("password", out _), Is.True);
            Assert.That(authentication.GetProperty("responses").TryGetProperty("200", out _), Is.True);
            Assert.That(authentication.GetProperty("responses").TryGetProperty("400", out _), Is.True);
            Assert.That(authentication.GetProperty("responses").TryGetProperty("401", out _), Is.True);
            Assert.That(orderId.GetProperty("name").GetString(), Is.EqualTo("id"));
            Assert.That(orderId.GetProperty("in").GetString(), Is.EqualTo("path"));
            Assert.That(orderId.GetProperty("required").GetBoolean(), Is.True);
            Assert.That(orderId.GetProperty("schema").GetProperty("format").GetString(), Is.EqualTo("uuid"));
        });
    }

    [Test]
    public void DoesNotIncludeLambdaRouteDeploymentDestinationsOrConfigurationValues()
    {
        var json = document.RootElement.GetRawText();

        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("paths").EnumerateObject()
                .Any(path => path.Name.Equals("/customers/validate", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(json, Does.Not.Contain("x-amazon-apigateway"));
            Assert.That(json, Does.Not.Contain("arn:aws:"));
            Assert.That(json, Does.Not.Contain(TestWebApplicationFactory.JwtKey));
            Assert.That(json, Does.Not.Contain(TestWebApplicationFactory.JwtIssuer));
            Assert.That(json, Does.Not.Contain("Host=localhost"));
        });
    }

    private JsonElement Operation(string path, string method) =>
        document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);

    [Test]
    public void UserDeleteDescribesRequiredUuidQueryBearerAndEmptyResponse()
    {
        var operation = Operation("/Users", "delete");
        Assert.That(operation.GetProperty("security")[0].TryGetProperty("Bearer", out _), Is.True);
        foreach (var code in new[] { "204", "400", "401", "403", "404", "500" })
            Assert.That(operation.GetProperty("responses").TryGetProperty(code, out _), Is.True, code);
        var parameter = operation.GetProperty("parameters").EnumerateArray().Single();
        Assert.That(parameter.GetProperty("name").GetString(), Is.EqualTo("id"));
        Assert.That(parameter.GetProperty("in").GetString(), Is.EqualTo("query"));
        Assert.That(parameter.GetProperty("required").GetBoolean(), Is.True);
        Assert.That(parameter.GetProperty("schema").GetProperty("format").GetString(), Is.EqualTo("uuid"));
        Assert.That(operation.TryGetProperty("requestBody", out _), Is.False);
        Assert.That(operation.GetProperty("responses").GetProperty("204").TryGetProperty("content", out _), Is.False);
    }

    [Test]
    public void OwnUserPatchDescribesOptionalFieldsBearerAndResponses()
    {
        var operation = Operation("/Users", "patch");
        Assert.That(operation.GetProperty("security")[0].TryGetProperty("Bearer", out _), Is.True);
        foreach (var code in new[] { "204", "400", "401", "403", "404", "409", "500" })
            Assert.That(operation.GetProperty("responses").TryGetProperty(code, out _), Is.True, code);
        var schema = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("UpdateOwnUserRequest");
        Assert.That(schema.GetProperty("properties").EnumerateObject().Select(p => p.Name), Is.EquivalentTo(new[] { "name", "password" }));
        Assert.That(!schema.TryGetProperty("required", out var required) || required.GetArrayLength() == 0, Is.True);
        Assert.That(schema.GetProperty("additionalProperties").GetBoolean(), Is.False);
        Assert.That(schema.GetProperty("properties").GetProperty("name").GetProperty("maxLength").GetInt32(), Is.EqualTo(100));
        Assert.That(operation.GetProperty("responses").GetProperty("204").TryGetProperty("content", out _), Is.False);
    }

    [TestCase("/Users", "post", new[] { "401", "403" })]
    [TestCase("/Users", "get", new[] { "401", "403", "404" })]
    [TestCase("/Orders/{id}/status", "get", new[] { "401", "403", "404" })]
    [TestCase("/Orders/{id}/budget", "patch", new[] { "401", "403", "404", "422" })]
    public void DescribesApplicableResponses(string path, string method, string[] responses)
    {
        var operation = Operation(path, method);
        foreach (var response in responses)
            Assert.That(operation.GetProperty("responses").TryGetProperty(response, out _), Is.True, response);
    }

    [Test]
    public void CustomerStatusAppearsOnlyInResponseAndRolesRemainStrings()
    {
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var response = schemas.GetProperty("CustomerResponse").GetProperty("properties");
        var request = schemas.GetProperty("CreateCustomerRequest").GetProperty("properties");
        var user = schemas.GetProperty("CreateUserRequest").GetProperty("properties");
        Assert.Multiple(() =>
        {
            Assert.That(response.GetProperty("status").GetProperty("type").GetString(), Is.EqualTo("string"));
            Assert.That(request.TryGetProperty("status", out _), Is.False);
            Assert.That(user.GetProperty("role").GetProperty("type").GetString(), Is.EqualTo("string"));
            Assert.That(user.GetProperty("role").GetProperty("description").GetString(), Does.Contain("Admin").And.Contain("Mechanic"));
            Assert.That(user.TryGetProperty("id", out _), Is.False);
        });
    }
}
