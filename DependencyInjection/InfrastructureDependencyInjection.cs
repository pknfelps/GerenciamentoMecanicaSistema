using Amazon.Lambda;
using Amazon.Runtime;
using Infrastructure.Authentication;
using Infrastructure.Email;
using Service.Interface.Authentication;
using Service.Interface.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool usesAwsConfiguration, Func<IServiceProvider, CancellationToken>? requestCancellation = null)
    {
        services.AddTransient<ITokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        var email = EmailInvocationConfiguration.Read(configuration, usesAwsConfiguration);
        services.AddSingleton(email);
        services.AddSingleton<IAmazonLambda>(_ => email.Endpoint is null
            ? new AmazonLambdaClient(email.ClientConfiguration())
            : new AmazonLambdaClient(new BasicAWSCredentials("local", "local"), email.ClientConfiguration()));
        services.AddScoped<IEmailSender>(provider => new LambdaEmailSender(provider.GetRequiredService<IAmazonLambda>(), email.FunctionName, () => requestCancellation?.Invoke(provider) ?? default));
        return services;
    }
}
