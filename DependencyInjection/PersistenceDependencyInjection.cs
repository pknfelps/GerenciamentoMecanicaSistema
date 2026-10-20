using Infrastructure.Observability;
using Infrastructure.Persistence.PostgreSql.HealthChecks;
using Infrastructure.Persistence.PostgreSql.Repositories;
using Infrastructure.Persistence.PostgreSql.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Service.Interface.Persistence;
using System.Data;

namespace DependencyInjection
{
    public static class PersistenceDependencyInjection
    {
        private const string DbConnectionString = "DefaultConnection";

        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(DbConnectionString)
                ?? throw new InvalidOperationException($"Connection string '{DbConnectionString}' not found.");

            services.AddScoped<IDbConnection>(services =>
            {
                var connection = new NpgsqlConnection(connectionString);
                try
                {
                    IntegrationMetrics.Measure(services.GetService<IntegrationMetrics>(), "postgresql", "connection.open", "application",
                        () => { connection.Open(); return true; });
                    return connection;
                }
                catch
                {
                    connection.Dispose();
                    throw;
                }
            });

            services.AddScoped<DbTransactionContext>();
            services.AddScoped<ITransactionManager, TransactionManager>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IStockRepository, StockRepository>();
            services.AddScoped<IVehicleRepository, VehicleRepository>();
            services.AddScoped<ICatalogRepository, CatalogRepository>();
            services.AddScoped<IOrdersRepository, OrdersRepository>();
            services.AddHealthChecks()
                .AddCheck<PostgreSqlHealthCheck>(
                    "postgresql",
                    tags: ["ready"],
                    timeout: TimeSpan.FromSeconds(2));

            return services;
        }
    }
}
