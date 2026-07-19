using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace GerenciamentoMecanicaSistema.HealthChecks
{
    public sealed class PostgreSqlHealthCheck(IConfiguration configuration) : IHealthCheck
    {
        private const string ConnectionStringName = "DefaultConnection";
        private const int ConnectionTimeoutSeconds = 2;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
                return HealthCheckResult.Unhealthy("PostgreSQL connection string is not configured.");

            try
            {
                var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Timeout = ConnectionTimeoutSeconds
                };

                await using var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
                await connection.OpenAsync(cancellationToken);

                return HealthCheckResult.Healthy();
            }
            catch (Exception exception) when (exception is NpgsqlException or TimeoutException or InvalidOperationException or ArgumentException)
            {
                return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
            }
        }
    }
}
