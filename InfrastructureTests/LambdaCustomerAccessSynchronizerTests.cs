using Amazon.Lambda;
using Amazon.Lambda.Model;
using Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Service.Interface.CustomerAccess;
using System.Text;
using System.Text.Json;

namespace InfrastructureTests;

public class LambdaCustomerAccessSynchronizerTests
{
    private const string CustomerId = "d0ac1d53-3d3e-4b04-be7b-9ec9bb5aaf45";
    private const string OtherCustomerId = "3853edf9-62ee-490e-a27c-adb268064107";
    private const long Revision = 5;
    private static readonly CustomerAccessCommand Command = new(Guid.Parse(CustomerId), Revision, null);

    [TestCase(1, CustomerId, Revision, "applied", CustomerAccessSyncOutcome.Applied)]
    [TestCase(1, CustomerId, Revision, "document-conflict", CustomerAccessSyncOutcome.DocumentConflict)]
    [TestCase(1, CustomerId, Revision, "unknown", CustomerAccessSyncOutcome.Pending)]
    [TestCase(2, CustomerId, Revision, "applied", CustomerAccessSyncOutcome.Pending)]
    [TestCase(1, OtherCustomerId, Revision, "applied", CustomerAccessSyncOutcome.Pending)]
    [TestCase(1, CustomerId, Revision - 1, "applied", CustomerAccessSyncOutcome.Pending)]
    public async Task AcceptsOnlyKnownOutcomesForTheRequestedCustomerRevision(
        int version, string customerId, long revision, string outcome, CustomerAccessSyncOutcome expected)
    {
        var json = JsonSerializer.Serialize(new { version, customerId, revision, outcome });

        var result = await SynchronizeAsync(json);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("{")]
    [TestCase("[]")]
    public async Task MalformedPayloadLeavesTheSynchronizationPending(string json)
    {
        var result = await SynchronizeAsync(json);

        Assert.That(result, Is.EqualTo(CustomerAccessSyncOutcome.Pending));
    }

    [TestCase("http://remote.example.invalid:3002")]
    [TestCase("http://localhost:3002/auth")]
    [TestCase("https://localhost:3002")]
    public void RejectsEndpointsOutsideTheLocalEmulator(string endpoint)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CustomerAccess:InvocationEndpoint"] = endpoint
        }).Build();

        Assert.Throws<InvalidOperationException>(() =>
            LambdaCustomerAccessSynchronizer.Create(configuration, false, () => CancellationToken.None));
    }

    private static async Task<CustomerAccessSyncOutcome> SynchronizeAsync(string json)
    {
        var lambda = Substitute.For<IAmazonLambda>();
        lambda.InvokeAsync(Arg.Any<InvokeRequest>(), Arg.Any<CancellationToken>()).Returns(_ => new InvokeResponse
        {
            StatusCode = 200,
            Payload = new MemoryStream(Encoding.UTF8.GetBytes(json))
        });
        using var client = new LambdaCustomerAccessSynchronizer(lambda, "AuthFunction");

        return await client.SynchronizeAsync(Command);
    }
}
