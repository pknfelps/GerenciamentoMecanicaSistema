using Infrastructure.Email;
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
            var emailSettings = configuration.GetSection("EmailSettings");

            service.Configure<EmailSettings>(settings =>
            {
                settings.Host = emailSettings["Host"] ?? string.Empty;
                settings.Port = int.TryParse(emailSettings["Port"], out var port) ? port : 0;
                settings.Username = emailSettings["Username"] ?? string.Empty;
                settings.Password = emailSettings["Password"] ?? string.Empty;
                settings.SenderName = emailSettings["SenderName"] ?? string.Empty;
                settings.SenderEmail = emailSettings["SenderEmail"] ?? string.Empty;
                settings.UseTls = bool.TryParse(emailSettings["UseTls"], out var useTls) && useTls;
            });

            var connectionString = configuration.GetConnectionString(DbConnectionString)
                ?? throw new InvalidOperationException($"Connection string '{DbConnectionString}' not found.");

            service.AddScoped<IDbConnection>(provider =>
            {
                var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                return connection;
            });

            service.AddScoped<DbTransactionContext>();
            service.AddScoped<ITransactionManager, TransactionManager>();
            service.AddScoped<ICustomerRepository, CustomerRepository>();
            service.AddScoped<IUserRepository, UserRepository>();
            service.AddScoped<IStockRepository, StockRepository>();
            service.AddScoped<IVehicleRepository, VehicleRepository>();
            service.AddScoped<ICatalogRepository, CatalogRepository>();
            service.AddScoped<IOrdersRepository, OrdersRepository>();
        }
    }
}
