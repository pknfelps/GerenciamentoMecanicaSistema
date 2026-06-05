using Dapper;
using Npgsql;
using System.Data;
using Testcontainers.PostgreSql;

namespace RepositoryTests
{
    public abstract class BaseRepositoryTests
    {
        protected PostgreSqlContainer PostgresContainer { get; set; }
        protected IDbConnection Connection { get; set; }

        [SetUp]
        public async Task SetUp()
        {
            PostgresContainer = new PostgreSqlBuilder("postgres:18")
                .WithDatabase("postgres")
                .WithUsername("postgres")
                .WithPassword("adm123")
                .Build();

            await PostgresContainer.StartAsync();

            Connection = new NpgsqlConnection(PostgresContainer.GetConnectionString());
            Connection.Open();

            await InternalSetup();
        }

        protected abstract Task InternalSetup();

        [TearDown]
        public async Task TearDown()
        {
            await PostgresContainer.DisposeAsync();
            Connection.Dispose();
        }
    }
}
