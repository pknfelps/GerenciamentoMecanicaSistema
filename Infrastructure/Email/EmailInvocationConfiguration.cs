using Amazon;
using Amazon.Lambda;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Email;

public sealed class EmailInvocationConfiguration
{
    private EmailInvocationConfiguration(string name, Uri? endpoint) { FunctionName = name; Endpoint = endpoint; }
    public string FunctionName { get; }
    public Uri? Endpoint { get; }
    public static EmailInvocationConfiguration Read(IConfiguration configuration, bool aws)
    {
        if (aws)
        {
            var environment = configuration["Runtime:Environment"];
            var expected = $"arn:aws:lambda:us-east-1:121754142617:function:mecanica-{environment}-email:live";
            if (environment is not ("hom" or "prd") || configuration["Email:FunctionName"] != expected
                || configuration["Email:ContractVersion"] != "1" || configuration["Email:InvocationEndpoint"] is not null)
                throw Invalid();
            return new(expected, null);
        }
        var address = configuration["Email:InvocationEndpoint"] ?? "http://127.0.0.1:3001";
        var name = configuration["Email:FunctionName"] ?? "EmailFunction";
        if (!Uri.TryCreate(address, UriKind.Absolute, out var endpoint) || endpoint.Scheme != "http"
            || endpoint.Host is not ("127.0.0.1" or "localhost" or "host.docker.internal") || endpoint.UserInfo.Length > 0
            || endpoint.AbsolutePath != "/" || endpoint.Query.Length > 0 || endpoint.Fragment.Length > 0 || name != "EmailFunction")
            throw Invalid();
        return new(name, endpoint);
    }
    public AmazonLambdaConfig ClientConfiguration()
    {
        var config = new AmazonLambdaConfig
        {
            AuthenticationRegion = "us-east-1",
            IgnoreConfiguredEndpointUrls = true,
            RetryMode = RequestRetryMode.Standard,
            MaxErrorRetry = 0,
            Timeout = TimeSpan.FromSeconds(5),
            LogResponse = false,
            LogMetrics = false
        };
        if (Endpoint is null) config.RegionEndpoint = RegionEndpoint.USEast1;
        else config.ServiceURL = Endpoint.ToString();
        return config;
    }
    private static InvalidOperationException Invalid() => new("Configuração de invocação de e-mail inválida.");
}
