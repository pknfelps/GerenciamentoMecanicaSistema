using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Globalization;
using System.Text.Json;

namespace Infrastructure.Configuration;

public sealed class AwsRuntimeConfigurationLoader(
    IAmazonSimpleSystemsManagement parameters,
    IAmazonSecretsManager secrets)
{
    private const string EnvironmentKey = "Runtime:Environment";
    private static readonly TimeSpan InitializationTimeout = TimeSpan.FromSeconds(45);

    public static async Task<Dictionary<string, string?>?> LoadIfConfiguredAsync(
        IConfiguration configuration,
        string certificatePath,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.AsEnumerable().Any(entry =>
            string.Equals(entry.Key, EnvironmentKey, StringComparison.OrdinalIgnoreCase)))
            return null;

        var environment = configuration[EnvironmentKey];
        ValidateEnvironment(environment);

        // Default SDK credentials include EKS Pod Identity; no credentials are stored here.
        try
        {
            using var parameterClient = new AmazonSimpleSystemsManagementClient(new AmazonSimpleSystemsManagementConfig
            {
                RetryMode = RequestRetryMode.Standard,
                MaxErrorRetry = 2
            });
            using var secretClient = new AmazonSecretsManagerClient(new AmazonSecretsManagerConfig
            {
                RetryMode = RequestRetryMode.Standard,
                MaxErrorRetry = 2
            });
            return await new AwsRuntimeConfigurationLoader(parameterClient, secretClient)
                .LoadAsync(environment!, certificatePath, cancellationToken);
        }
        catch (AmazonClientException)
        {
            // SDK exceptions can contain response bodies or configuration values.
            throw new InvalidOperationException("Unable to initialize AWS runtime clients. Check AWS region and credentials.");
        }
    }

    public async Task<Dictionary<string, string?>> LoadAsync(
        string environment,
        string certificatePath,
        CancellationToken cancellationToken = default)
    {
        ValidateEnvironment(environment);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(InitializationTimeout);
        var stage = "read AWS runtime parameters";

        try
        {
            var basePrefix = $"/mecanica/{environment}/base/v2/";
            var databasePrefix = $"/mecanica/{environment}/database/v2/";
            string[] names =
            [
                basePrefix + "jwt-secret-arn", basePrefix + "jwt-issuer", basePrefix + "jwt-audience",
                databasePrefix + "endpoint", databasePrefix + "port", databasePrefix + "database-name",
                databasePrefix + "api-secret-arn", databasePrefix + "api-db-user", databasePrefix + "ssl-mode"
            ];
            var response = await parameters.GetParametersAsync(new GetParametersRequest
            {
                Names = [.. names],
                WithDecryption = false
            }, deadline.Token).WaitAsync(deadline.Token);

            var values = ReadParameters(response, names);
            var port = ReadPort(values[databasePrefix + "port"]);
            if (values[databasePrefix + "api-db-user"] != "mecanica_api")
                throw new ConfigurationValidationException("Database API user parameter must be mecanica_api.");
            if (values[databasePrefix + "ssl-mode"] != "verify-full")
                throw new ConfigurationValidationException("Database SSL mode parameter must be verify-full.");
            if (!File.Exists(certificatePath))
                throw new ConfigurationValidationException("RDS root certificate is missing from the published application.");

            stage = "read JWT secret";
            var jwt = await ReadSecretAsync(values[basePrefix + "jwt-secret-arn"], deadline.Token);
            using var jwtDocument = ParseSecret(jwt, "JWT");
            var key = ReadSecretField(jwtDocument.RootElement, "key", "JWT");
            if (key.Length < 32)
                throw new ConfigurationValidationException("JWT signing key must contain at least 32 characters.");

            stage = "read database API secret";
            var database = await ReadSecretAsync(values[databasePrefix + "api-secret-arn"], deadline.Token);
            using var databaseDocument = ParseSecret(database, "Database API");
            var username = ReadSecretField(databaseDocument.RootElement, "username", "Database API");
            var password = ReadSecretField(databaseDocument.RootElement, "password", "Database API");
            if (username != values[databasePrefix + "api-db-user"])
                throw new ConfigurationValidationException("Database API secret username does not match mecanica_api.");

            stage = "configure the RDS connection";
            var connection = new NpgsqlConnectionStringBuilder
            {
                Host = values[databasePrefix + "endpoint"],
                Port = port,
                Database = values[databasePrefix + "database-name"],
                Username = username,
                Password = password,
                SslMode = SslMode.VerifyFull,
                RootCertificate = certificatePath,
                GssEncryptionMode = GssEncryptionMode.Disable
            };

            deadline.Token.ThrowIfCancellationRequested();
            // Publish all four values together; failed reads never leave partial configuration.
            return new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = values[basePrefix + "jwt-issuer"],
                ["Jwt:Audience"] = values[basePrefix + "jwt-audience"],
                ["ConnectionStrings:DefaultConnection"] = connection.ConnectionString
            };
        }
        catch (ConfigurationValidationException exception)
        {
            throw new InvalidOperationException(exception.Message);
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException("AWS runtime configuration loading was canceled or exceeded the 45-second deadline.");
        }
        catch (Exception)
        {
            // Do not retain inner exceptions: neither secrets nor full connection strings belong in startup logs.
            throw new InvalidOperationException($"Unable to {stage}. Check AWS runtime configuration and access.");
        }
    }

    private async Task<string?> ReadSecretAsync(string arn, CancellationToken cancellationToken)
    {
        var response = await secrets.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = arn,
            VersionStage = "AWSCURRENT"
        }, cancellationToken).WaitAsync(cancellationToken);
        return response.SecretString;
    }

    private static Dictionary<string, string> ReadParameters(GetParametersResponse response, string[] names)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            var matches = response.Parameters?.Where(parameter => parameter.Name == name).ToArray();
            if (matches is not { Length: 1 } || matches[0].Type != ParameterType.String
                || string.IsNullOrWhiteSpace(matches[0].Value))
                throw new ConfigurationValidationException($"Required String parameter is missing or invalid: {name}.");
            values.Add(name, matches[0].Value);
        }

        if (response.InvalidParameters is { Count: > 0 })
            throw new ConfigurationValidationException("AWS reported invalid runtime parameter names.");
        return values;
    }

    private static int ReadPort(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port is < 1 or > 65535)
            throw new ConfigurationValidationException("Database port parameter is invalid.");
        return port;
    }

    private static JsonDocument ParseSecret(string? json, string label)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ConfigurationValidationException($"{label} secret must contain a JSON object.");
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new ConfigurationValidationException($"{label} secret contains invalid JSON.");
        }
    }

    private static string ReadSecretField(JsonElement secret, string field, string label)
    {
        if (secret.ValueKind != JsonValueKind.Object || !secret.TryGetProperty(field, out var value)
            || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new ConfigurationValidationException($"{label} secret field '{field}' is missing or invalid.");
        return value.GetString()!;
    }

    private static void ValidateEnvironment(string? environment)
    {
        if (environment is not ("hom" or "prd"))
            throw new InvalidOperationException("Runtime:Environment must be hom or prd when configured.");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3871",
        Justification = "Private validation marker is converted to a sanitized InvalidOperationException before leaving the loader.")]
    private sealed class ConfigurationValidationException(string message) : Exception(message);
}
