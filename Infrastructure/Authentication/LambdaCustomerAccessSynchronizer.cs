using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Infrastructure.Observability;
using Microsoft.Extensions.Configuration;
using Service.Interface.CustomerAccess;
using System.Text.Json;

namespace Infrastructure.Authentication;

public sealed class LambdaCustomerAccessSynchronizer(
    IAmazonLambda lambda, string functionName, Func<CancellationToken>? requestCancellation = null, IntegrationMetrics? metrics = null)
    : ICustomerAccessSynchronizer, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static LambdaCustomerAccessSynchronizer Create(IConfiguration configuration, bool aws,
        Func<CancellationToken> cancellation, IntegrationMetrics? metrics = null)
    {
        var endpoint = configuration["CustomerAccess:InvocationEndpoint"];
        var environment = configuration["Runtime:Environment"];
        var client = new AmazonLambdaConfig
        {
            AuthenticationRegion = "us-east-1",
            IgnoreConfiguredEndpointUrls = true,
            RetryMode = RequestRetryMode.Standard,
            MaxErrorRetry = 0,
            Timeout = TimeSpan.FromSeconds(5),
            LogResponse = false,
            LogMetrics = false
        };
        if (aws)
        {
            if (environment is not ("hom" or "prd") || endpoint is not null)
                throw new InvalidOperationException("Configuração da AuthFunction inválida.");
            client.RegionEndpoint = RegionEndpoint.USEast1;
            return new(new AmazonLambdaClient(client),
                $"arn:aws:lambda:us-east-1:121754142617:function:mecanica-{environment}-auth:live", cancellation, metrics);
        }
        if (!Uri.TryCreate(endpoint ?? "http://127.0.0.1:3002", UriKind.Absolute, out var local) || local.Scheme != "http"
            || local.Host is not ("127.0.0.1" or "localhost" or "host.docker.internal") || local.UserInfo.Length > 0
            || local.AbsolutePath != "/" || local.Query.Length > 0 || local.Fragment.Length > 0)
            throw new InvalidOperationException("Configuração local da AuthFunction inválida.");
        client.ServiceURL = local.ToString();
        return new(new AmazonLambdaClient(new BasicAWSCredentials("local", "local"), client), "AuthFunction", cancellation, metrics);
    }

    public async Task<CustomerAccessSyncOutcome> SynchronizeAsync(CustomerAccessCommand command)
    {
        var callerCancellation = requestCancellation?.Invoke() ?? default;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(callerCancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            using var payload = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(command, JsonOptions));
            var request = new InvokeRequest
            {
                FunctionName = functionName,
                InvocationType = InvocationType.RequestResponse,
                LogType = LogType.None,
                PayloadStream = payload
            };
            var result = await IntegrationMetrics.MeasureAsync(metrics, "lambda", "invoke_auth", "application",
                () => lambda.InvokeAsync(request, deadline.Token).WaitAsync(deadline.Token), callerCancellation);
            using var response = result.Payload;
            deadline.Token.ThrowIfCancellationRequested();
            return result.StatusCode == 200 && string.IsNullOrEmpty(result.FunctionError) && response is { Length: > 0 and <= 1024 }
                ? ReadOutcome(response, command) : CustomerAccessSyncOutcome.Pending;
        }
        catch (Exception error) when (error is AmazonServiceException or AmazonClientException or OperationCanceledException
            or TimeoutException or IOException or HttpRequestException or JsonException)
        {
            return CustomerAccessSyncOutcome.Pending;
        }
    }

    private static CustomerAccessSyncOutcome ReadOutcome(Stream response, CustomerAccessCommand command)
    {
        using var document = JsonDocument.Parse(response);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 4
            || !root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out var contract) || contract != CustomerAccessCommand.ContractVersion
            || !root.TryGetProperty("customerId", out var id) || id.ValueKind != JsonValueKind.String
            || !id.TryGetGuid(out var customerId) || customerId != command.CustomerId
            || !root.TryGetProperty("revision", out var revision) || revision.ValueKind != JsonValueKind.Number
            || !revision.TryGetInt64(out var confirmed) || confirmed != command.Revision
            || !root.TryGetProperty("outcome", out var result) || result.ValueKind != JsonValueKind.String)
            return CustomerAccessSyncOutcome.Pending;
        return result.GetString() switch
        {
            "applied" => CustomerAccessSyncOutcome.Applied,
            "document-conflict" => CustomerAccessSyncOutcome.DocumentConflict,
            _ => CustomerAccessSyncOutcome.Pending
        };
    }

    public void Dispose() => lambda.Dispose();
}
