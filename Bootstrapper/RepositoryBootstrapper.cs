using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Repository;
using Repository.Interface;
using System.Data;

namespace Bootstrapper
{
    public class RepositoryBootstrapper
    {
        private const string ConnectionString = "host=host.docker.internal;port=5432;database=postgres;User Id=postgres;password=adm123;";

        public static async void Register(IServiceCollection service)
        {
            service.AddScoped<IDbConnection>(provider =>
            {
                var connection = new NpgsqlConnection(ConnectionString);
                connection.Open();
                return connection;
            });

            service.AddScoped<IClienteRepository, ClienteRepository>();
        }
    }
}
