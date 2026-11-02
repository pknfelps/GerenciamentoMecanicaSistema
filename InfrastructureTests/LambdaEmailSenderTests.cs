using Amazon.Lambda;
using Amazon.Lambda.Model;
using Microsoft.Extensions.Configuration;
using Infrastructure.Email;
using NSubstitute;
using System.Text;
using System.Text.Json;

namespace InfrastructureTests;

public class LambdaEmailSenderTests
{
    private const string Arn = "arn:aws:lambda:us-east-1:121754142617:function:mecanica-hom-email:live";
    private IAmazonLambda lambda = null!;
    private LambdaEmailSender sender = null!;
    [SetUp]
    public void Setup()
    {
        lambda = Substitute.For<IAmazonLambda>();
        sender = new LambdaEmailSender(lambda, Arn);
        lambda.InvokeAsync(Arg.Any<InvokeRequest>(), Arg.Any<CancellationToken>()).Returns(_ => Response("{\"version\":1,\"outcome\":\"captured\"}"));
    }
    [TearDown]
    public void TearDown() => lambda.Dispose();

    private static InvokeResponse Response(string json, int status = 200, string? functionError = null) => new()
    { StatusCode = status, FunctionError = functionError, Payload = new MemoryStream(Encoding.UTF8.GetBytes(json)) };

    [Test]
    public async Task SendsOneSynchronousRequestWithTheSuppliedMessageAndNoLogs()
    {
        byte[]? sent = null;
        lambda.InvokeAsync(Arg.Any<InvokeRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<InvokeRequest>();
            Assert.That(request.FunctionName, Is.EqualTo(Arn));
            Assert.That(request.InvocationType, Is.EqualTo(InvocationType.RequestResponse));
            Assert.That(request.LogType, Is.EqualTo(LogType.None));
            sent = request.PayloadStream.ToArray();
            return Response("{\"version\":1,\"outcome\":\"captured\"}");
        });
        await sender.SendAsync("budget_available", "Cliente <b>&", "customer@example.invalid", "Assunto", "Texto", "<p>Texto seguro</p>", CancellationToken.None);
        using var json = JsonDocument.Parse(sent!);
        Assert.That(json.RootElement.GetProperty("htmlBody").GetString(), Is.EqualTo("<p>Texto seguro</p>"));
        Assert.That(json.RootElement.GetProperty("type").GetString(), Is.EqualTo("budget_available"));
        Assert.That(json.RootElement.EnumerateObject().Count(), Is.EqualTo(7));
        await lambda.Received(1).InvokeAsync(Arg.Any<InvokeRequest>(), Arg.Any<CancellationToken>());
    }

    [TestCase(202, null)]
    [TestCase(200, "Unhandled")]
    public void TransportStatusOrFunctionErrorCannotConfirmCapture(int status, string? functionError)
    {
        lambda.InvokeAsync(Arg.Any<InvokeRequest>(), Arg.Any<CancellationToken>()).Returns(_ => Response("{\"version\":1,\"outcome\":\"captured\"}", status, functionError));
        Assert.ThrowsAsync<EmailInvocationException>(() => sender.SendAsync("status_changed", "Cliente", "customer@example.invalid", "Assunto", "Texto", "<p>Texto</p>", CancellationToken.None));
    }

    [TestCase("")]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{")]
    [TestCase("{\"version\":2,\"outcome\":\"captured\"}")]
    [TestCase("{\"version\":1,\"outcome\":\"captured\",\"error\":\"sensitive\"}")]
    [TestCase("{\"version\":1,\"version\":1,\"outcome\":\"captured\"}")]
    [TestCase("{\"version\":\"1\",\"outcome\":\"captured\"}")]
    [TestCase("{\"version\":1,\"outcome\":\"failure\",\"error\":\"smtp-unavailable\"}")]
    public void InvalidOrFailedResponseNeverBecomesCapture(string payload)
    {
        lambda.InvokeAsync(Arg.Any<InvokeRequest>(), Arg.Any<CancellationToken>()).Returns(_ => Response(payload));
        var error = Assert.ThrowsAsync<EmailInvocationException>(() => sender.SendAsync("status_changed", "Cliente", "customer@example.invalid", "Assunto", "Texto", "<p>Texto</p>", CancellationToken.None));
        Assert.That(error!.Message, Does.Not.Contain("sensitive"));
        Assert.That(error.InnerException, Is.Null);
    }

    [Test]
    public void KnownRemoteTimeoutRemainsATimeoutWithFixedMessage()
    {
        lambda.InvokeAsync(Arg.Any<InvokeRequest>(), Arg.Any<CancellationToken>()).Returns(_ => Response("{\"version\":1,\"outcome\":\"failure\",\"error\":\"timeout\"}"));
        Assert.ThrowsAsync<TimeoutException>(() => sender.SendAsync("status_changed", "Cliente", "customer@example.invalid", "Assunto", "Texto", "<p>Texto</p>", CancellationToken.None));
    }

    [Test]
    public async Task CallerCancellationStopsInvokeWithoutRetryOrLeakingSdkContent()
    {
        var observed = CancellationToken.None;
        lambda.InvokeAsync(Arg.Any<InvokeRequest>(), Arg.Any<CancellationToken>()).Returns(async call =>
        { observed = call.Arg<CancellationToken>(); await Task.Delay(Timeout.Infinite, observed); return Response(""); });
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var error = Assert.CatchAsync<OperationCanceledException>(() => sender.SendAsync("status_changed", "Cliente", "customer@example.invalid", "Assunto", "Texto", "<p>Texto</p>", cancellation.Token));
        Assert.That(observed.IsCancellationRequested, Is.True);
        Assert.That(error!.InnerException, Is.Null);
        await lambda.Received(1).InvokeAsync(Arg.Any<InvokeRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void SdkFailureIsSanitizedAndNotRetriedByTheAdapter()
    {
        lambda.InvokeAsync(Arg.Any<InvokeRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<InvokeResponse>(new AmazonLambdaException("secret-in-sdk-message")));
        var error = Assert.ThrowsAsync<EmailInvocationException>(() => sender.SendAsync("status_changed", "Cliente", "customer@example.invalid", "Assunto", "Texto", "<p>Texto</p>", CancellationToken.None));
        Assert.That(error!.Message, Does.Not.Contain("secret-in-sdk-message"));
        Assert.That(error.InnerException, Is.Null);
    }

    [Test]
    public void ClientDisablesRetriesAndPayloadLogging()
    {
        var config = EmailInvocationConfiguration.Read(new ConfigurationBuilder().Build(), false).ClientConfiguration();
        Assert.That(config.MaxErrorRetry, Is.Zero);
        Assert.That(config.LogResponse, Is.False);
        Assert.That(config.LogMetrics, Is.False);
        Assert.That(config.Timeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
    }

    [TestCase("hom")]
    [TestCase("prd")]
    public void AwsDiscoveryAllowsOnlyTheExactAliasAndNoEndpointOverride(string environment)
    {
        var values = new Dictionary<string, string?>
        {
            ["Runtime:Environment"] = environment,
            ["Email:FunctionName"] = Arn.Replace("-hom-", "-" + environment + "-"),
            ["Email:ContractVersion"] = "1"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        Assert.That(EmailInvocationConfiguration.Read(configuration, true).Endpoint, Is.Null);
        configuration["Email:InvocationEndpoint"] = "http://localhost:3001";
        Assert.Throws<InvalidOperationException>(() => EmailInvocationConfiguration.Read(configuration, true));
        configuration["Email:InvocationEndpoint"] = null;
        configuration["Email:ContractVersion"] = "2";
        Assert.Throws<InvalidOperationException>(() => EmailInvocationConfiguration.Read(configuration, true));
    }

    [TestCase("http://127.0.0.1:3001", true)]
    [TestCase("http://host.docker.internal:3001", true)]
    [TestCase("http://remote.example.invalid", false)]
    [TestCase("http://user:password@localhost:3001", false)]
    [TestCase("http://localhost:3001?key=value", false)]
    public void LocalEndpointIsRestrictedToTheLocalEmulator(string endpoint, bool valid)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Email:InvocationEndpoint"] = endpoint }).Build();
        if (valid) Assert.That(EmailInvocationConfiguration.Read(configuration, false).ClientConfiguration().MaxErrorRetry, Is.Zero);
        else Assert.Throws<InvalidOperationException>(() => EmailInvocationConfiguration.Read(configuration, false));
    }
}
