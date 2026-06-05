using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Repository;
using Repository.Interface;
using System.Data;

namespace DependencyInjection
{
    public static class RepositoryDependencyInjection
    {
        private const string DbConnectionString = "DefaultConnection";

        public static void Register(IServiceCollection service, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(DbConnectionString)
                ?? throw new InvalidOperationException($"Connection string '{DbConnectionString}' not found.");

            service.AddScoped<IDbConnection>(provider =>
            {
                var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                return connection;
            });

            service.AddScoped<IClienteRepository, ClienteRepository>();
            service.AddScoped<IUsuarioRepository, UsuarioRepository>();
            service.AddScoped<IStockRepository, StockRepository>();
            service.AddScoped<IVehicleRepository, VehicleRepository>();
        }
    }
}
