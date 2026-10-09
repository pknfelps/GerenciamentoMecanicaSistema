using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NSubstitute;
using System.Text.Json;

namespace InfrastructureTests;

public class AwsRuntimeConfigurationLoaderTests
{
    private const string JwtArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:/mecanica/hom/base/jwt-test00";
    private const string DatabaseArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:/mecanica/hom/database/api-test00";
    private const string FakeKey = "tests-only-jwt-key-with-at-least-32-characters";
    private const string FakePassword = "tests-only-password;with=connection-string-characters";
    private IAmazonSimpleSystemsManagement _parameters = null!;
    private IAmazonSecretsManager _secrets = null!;
    private AwsRuntimeConfigurationLoader _loader = null!;
    private GetParametersResponse _response = null!;
    private string? _jwtJson;
    private string? _databaseJson;
    private string _certificatePath = null!;

    [SetUp]
    public void SetUp()
    {
        _parameters = Substitute.For<IAmazonSimpleSystemsManagement>();
        _secrets = Substitute.For<IAmazonSecretsManager>();
        _loader = new AwsRuntimeConfigurationLoader(_parameters, _secrets);
        _certificatePath = Path.Combine(AppContext.BaseDirectory, "Certificates", "rds-ca.pem");
        _jwtJson = JsonSerializer.Serialize(new { key = FakeKey });
        _databaseJson = JsonSerializer.Serialize(new
        {
            username = "mecanica_api",
            password = FakePassword,
            host = "obsolete-host.invalid",
            port = 1234,
            dbname = "obsolete_database"
        });
        var values = new Dictionary<string, string>
        {
            ["base/v2/jwt-secret-arn"] = JwtArn,
            ["base/v2/jwt-issuer"] = "mecanica-hom-auth",
            ["base/v2/jwt-audience"] = "mecanica-hom-api",
            ["database/v2/endpoint"] = "current-rds.example.invalid",
            ["database/v2/port"] = "5432",
            ["database/v2/database-name"] = "mecanica",
            ["database/v2/api-secret-arn"] = DatabaseArn,
            ["database/v2/api-db-user"] = "mecanica_api",
            ["database/v2/ssl-mode"] = "verify-full"
        };
        _response = new GetParametersResponse
        {
            Parameters = values.Select(value => new Parameter
            {
                Name = "/mecanica/hom/" + value.Key,
                Value = value.Value,
                Type = ParameterType.String
            }).ToList()
        };
        _parameters.GetParametersAsync(Arg.Any<GetParametersRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(_response));
        _secrets.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(new GetSecretValueResponse
            {
                SecretString = call.Arg<GetSecretValueRequest>().SecretId == JwtArn ? _jwtJson : _databaseJson
            }));
    }

    [TearDown]
    public void TearDown()
    {
        _parameters.Dispose();
        _secrets.Dispose();
    }

    [Test]
    public async Task LoadAsync_UsesCurrentParametersAndEscapesCredentialsWithVerifiedTls()
    {
        var result = await LoadAsync();
        var connection = new NpgsqlConnectionStringBuilder(result["ConnectionStrings:DefaultConnection"]);

        Assert.Multiple(() =>
        {
            Assert.That(result["Jwt:Key"], Is.EqualTo(FakeKey));
            Assert.That(result["Jwt:Issuer"], Is.EqualTo("mecanica-hom-auth"));
            Assert.That(result["Jwt:Audience"], Is.EqualTo("mecanica-hom-api"));
            Assert.That(connection.Host, Is.EqualTo("current-rds.example.invalid"));
            Assert.That(connection.Port, Is.EqualTo(5432));
            Assert.That(connection.Database, Is.EqualTo("mecanica"));
            Assert.That(connection.Username, Is.EqualTo("mecanica_api"));
            Assert.That(connection.Password, Is.EqualTo(FakePassword));
            Assert.That(connection.SslMode, Is.EqualTo(SslMode.VerifyFull));
            Assert.That(connection.GssEncryptionMode, Is.EqualTo(GssEncryptionMode.Disable));
            Assert.That(connection.RootCertificate, Is.EqualTo(_certificatePath));
            Assert.That(connection.Pooling, Is.True);
            Assert.That(connection.Timeout, Is.EqualTo(15));
            Assert.That(connection.CommandTimeout, Is.EqualTo(30));
        });
        await _parameters.Received(1).GetParametersAsync(Arg.Is<GetParametersRequest>(request =>
            request.Names.Count == 9 && request.Names.All(name => name.StartsWith("/mecanica/hom/"))
            && request.WithDecryption == false), Arg.Any<CancellationToken>());
        await _secrets.Received(1).GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(request =>
            request.SecretId == JwtArn && request.VersionStage == "AWSCURRENT"), Arg.Any<CancellationToken>());
        await _secrets.Received(1).GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(request =>
            request.SecretId == DatabaseArn && request.VersionStage == "AWSCURRENT"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LoadIfConfiguredAsync_LocalConfigurationDoesNotCreateAwsClients()
    {
        var configuration = LocalConfiguration();

        var result = await AwsRuntimeConfigurationLoader.LoadIfConfiguredAsync(configuration, "missing-certificate.pem");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(configuration["Jwt:Key"], Is.EqualTo("local-key"));
            Assert.That(configuration.GetConnectionString("DefaultConnection"), Is.EqualTo("Host=local"));
        });
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("development")]
    [TestCase("HOM")]
    [TestCase(null)]
    public void LoadIfConfiguredAsync_RejectsPresentInvalidEnvironment(string? environment)
    {
        var configuration = LocalConfiguration();
        configuration["Runtime:Environment"] = environment;

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            AwsRuntimeConfigurationLoader.LoadIfConfiguredAsync(configuration, _certificatePath));
    }

    [Test]
    public async Task LoadAsync_UsesPrdParameterNamespace()
    {
        foreach (var parameter in _response.Parameters)
            parameter.Name = parameter.Name.Replace("/hom/", "/prd/");

        await _loader.LoadAsync("prd", _certificatePath);

        await _parameters.Received(1).GetParametersAsync(Arg.Is<GetParametersRequest>(request =>
            request.Names.All(name => name.StartsWith("/mecanica/prd/"))), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LoadAsync_CloudValuesOverrideLocalConfigurationTogether()
    {
        var configuration = LocalConfiguration();
        configuration.AddInMemoryCollection(await LoadAsync());

        Assert.Multiple(() =>
        {
            Assert.That(configuration["Jwt:Key"], Is.EqualTo(FakeKey));
            Assert.That(configuration["Jwt:Issuer"], Is.EqualTo("mecanica-hom-auth"));
            Assert.That(configuration["Jwt:Audience"], Is.EqualTo("mecanica-hom-api"));
            Assert.That(configuration.GetConnectionString("DefaultConnection"), Does.Contain("current-rds.example.invalid"));
        });
    }

    [TestCase("base/v2/jwt-secret-arn")]
    [TestCase("base/v2/jwt-issuer")]
    [TestCase("base/v2/jwt-audience")]
    [TestCase("database/v2/endpoint")]
    [TestCase("database/v2/port")]
    [TestCase("database/v2/database-name")]
    [TestCase("database/v2/api-secret-arn")]
    [TestCase("database/v2/api-db-user")]
    [TestCase("database/v2/ssl-mode")]
    public async Task LoadAsync_RejectsMissingParameterBeforeReadingSecrets(string name)
    {
        _response.Parameters.Remove(Parameter(name));

        AssertSafeFailure();

        await _secrets.DidNotReceive().GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>());
    }

    [TestCase("database/v2/port", "not-a-port")]
    [TestCase("database/v2/port", "0")]
    [TestCase("database/v2/port", "65536")]
    [TestCase("database/v2/api-db-user", "mecanica_admin")]
    [TestCase("database/v2/ssl-mode", "require")]
    [TestCase("base/v2/jwt-issuer", " ")]
    [TestCase("base/v2/jwt-audience", "")]
    [TestCase("database/v2/endpoint", " ")]
    public void LoadAsync_RejectsInvalidParameterValues(string name, string value)
    {
        Parameter(name).Value = value;

        AssertSafeFailure();
    }

    [Test]
    public void LoadAsync_RejectsSecureStringParameter()
    {
        Parameter("base/v2/jwt-issuer").Type = ParameterType.SecureString;

        AssertSafeFailure();
    }

    [Test]
    public void LoadAsync_RequiresPublishedCertificate()
    {
        _certificatePath = Path.Combine(AppContext.BaseDirectory, "absent-certificate.pem");

        AssertSafeFailure();
    }

    [TestCase(null)]
    [TestCase("not-json")]
    [TestCase("[]")]
    [TestCase("{}")]
    [TestCase("{\"key\":123}")]
    [TestCase("{\"key\":\"short\"}")]
    [TestCase("{\"key\":\"                                  \"}")]
    public void LoadAsync_RejectsInvalidJwtSecret(string? json)
    {
        _jwtJson = json;

        AssertSafeFailure();
    }

    [TestCase(null)]
    [TestCase("not-json")]
    [TestCase("null")]
    [TestCase("{}")]
    [TestCase("{\"username\":\"mecanica_admin\",\"password\":\"fake\"}")]
    [TestCase("{\"username\":\"mecanica_api\",\"password\":\"\"}")]
    [TestCase("{\"username\":\"mecanica_api\",\"password\":123}")]
    public void LoadAsync_RejectsInvalidDatabaseSecret(string? json)
    {
        _databaseJson = json;

        AssertSafeFailure();
    }

    [Test]
    public void LoadAsync_ParameterFailureDoesNotExposeAwsResponse()
    {
        _parameters.GetParametersAsync(Arg.Any<GetParametersRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GetParametersResponse>(new AmazonSimpleSystemsManagementException(FakeKey)));

        AssertSafeFailure();
    }

    [TestCase(JwtArn)]
    [TestCase(DatabaseArn)]
    public void LoadAsync_SecretFailureDoesNotExposeAwsResponseOrChangeLocalConfiguration(string arn)
    {
        var configuration = LocalConfiguration();
        _secrets.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(request => request.SecretId == arn), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GetSecretValueResponse>(new AmazonSecretsManagerException(FakePassword)));

        AssertSafeFailure();
        Assert.That(configuration["Jwt:Key"], Is.EqualTo("local-key"));
    }

    [Test]
    public void LoadAsync_CancellationIsPassedToAwsAndStopsInitialization()
    {
        using var cancellation = new CancellationTokenSource();
        _parameters.GetParametersAsync(Arg.Any<GetParametersRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>())
                .ContinueWith<GetParametersResponse>(_ => throw new OperationCanceledException(), TaskScheduler.Default));
        cancellation.Cancel();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _loader.LoadAsync("hom", _certificatePath, cancellation.Token));

        Assert.That(exception!.Message, Does.Contain("canceled"));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public void LoadAsync_DeadlineStopsAnUnresponsiveAwsCall()
    {
        _parameters.GetParametersAsync(Arg.Any<GetParametersRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<GetParametersResponse>().Task);
        var clock = System.Diagnostics.Stopwatch.StartNew();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => LoadAsync());

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("45-second deadline"));
            Assert.That(exception.InnerException, Is.Null);
            Assert.That(clock.Elapsed.TotalSeconds, Is.InRange(44, 50));
        });
    }

    private Task<Dictionary<string, string?>> LoadAsync() => _loader.LoadAsync("hom", _certificatePath);

    private Parameter Parameter(string name) => _response.Parameters.Single(parameter => parameter.Name == "/mecanica/hom/" + name);

    private void AssertSafeFailure()
    {
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => LoadAsync());
        Assert.Multiple(() =>
        {
            Assert.That(exception!.InnerException, Is.Null);
            Assert.That(exception.ToString(), Does.Not.Contain(FakeKey));
            Assert.That(exception.ToString(), Does.Not.Contain(FakePassword));
            Assert.That(exception.ToString(), Does.Not.Contain("Password="));
        });
    }

    private static ConfigurationManager LocalConfiguration()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "local-key",
            ["Jwt:Issuer"] = "local-issuer",
            ["Jwt:Audience"] = "local-audience",
            ["ConnectionStrings:DefaultConnection"] = "Host=local"
        });
        return configuration;
    }
}
