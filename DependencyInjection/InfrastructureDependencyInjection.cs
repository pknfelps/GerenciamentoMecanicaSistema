using Amazon.Lambda;
using Amazon.Runtime;
using Infrastructure.Authentication;
using Infrastructure.Email;
using Infrastructure.Observability;
using Service.Interface.CustomerAccess;
using Service.Interface.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool usesAwsConfiguration, Func<IServiceProvider, CancellationToken>? requestCancellation = null)
    {
        var email = EmailInvocationConfiguration.Read(configuration, usesAwsConfiguration);
        services.AddSingleton(email);
        services.AddSingleton<IAmazonLambda>(_ => email.Endpoint is null
            ? new AmazonLambdaClient(email.ClientConfiguration())
            : new AmazonLambdaClient(new BasicAWSCredentials("local", "local"), email.ClientConfiguration()));
        services.AddScoped<IEmailSender>(provider => new LambdaEmailSender(provider.GetRequiredService<IAmazonLambda>(), email.FunctionName, () => requestCancellation?.Invoke(provider) ?? default));
        services.AddSingleton<ICustomerAccessSynchronizer>(provider =>
            LambdaCustomerAccessSynchronizer.Create(configuration, usesAwsConfiguration,
                () => requestCancellation?.Invoke(provider) ?? default, provider.GetService<IntegrationMetrics>()));
        return services;
    }
}
