using Dapper;
using Domain.Interface.Service;
using Repository.PersistenceModels;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class CatalogRepository(IDbConnection connection, DbTransactionContext? transactionContext = null) : BaseRepository(connection, transactionContext), ICatalogRepository
    {
        public static string RegisterServiceSql { get; private set; } = """
            INSERT INTO catalog(id, description, hours, price_per_hour)
            VALUES (@Id, @Description, @Hours, @PricePerHour);
            """;

        public static string GetServicesSql { get; private set; } = """
            SELECT id, description, hours, price_per_hour AS PricePerHour
            FROM catalog
            {0}
            LIMIT 50;
            """;

        public static string UpdateServiceSql { get; private set; } = """
            UPDATE catalog
                SET description = @Description,
                    hours = @Hours,
                    price_per_hour = @PricePerHour
            Where id = @Id;
            """;

        public static string DeleteServiceSql { get; private set; } = """
            DELETE FROM catalog
            Where id = @Id;
            """;

        public async Task<int> RegisterService(IMechanicalService service)
        {
            return await Connection.ExecuteAsync(RegisterServiceSql, MechanicalServiceDb.Create(service), Transaction);
        }

        public async Task<IEnumerable<IMechanicalService>> GetServices(Guid? id = null, string description = "")
        {
            var query = GetServicesSql.BuildQuery(BuildQueryParameters(id, description));
            var catalog = await Connection.QueryAsync<MechanicalServiceDb>(query.Sql, query.Parameters, Transaction);

            return catalog.Select(service => service.ToDomain());
        }

        public async Task<IMechanicalService?> GetService(Guid? id = null, string description = "")
        {
            var query = GetServicesSql.BuildQuery(BuildQueryParameters(id, description));
            var service = await Connection.QuerySingleOrDefaultAsync<MechanicalServiceDb>(query.Sql, query.Parameters, Transaction);

            if (service == null)
                return null;

            return service.ToDomain();
        }

        public async Task<int> UpdateService(IMechanicalService service)
        {
            return await Connection.ExecuteAsync(UpdateServiceSql, MechanicalServiceDb.Create(service), Transaction);
        }

        public async Task<int> DeleteService(Guid serviceId)
        {
            return await Connection.ExecuteAsync(DeleteServiceSql, new { Id = serviceId }, Transaction);
        }

        private static Dictionary<string, object?> BuildQueryParameters(Guid? id = null, string description = "")
        {
            return new() { { nameof(id), id }, { nameof(description), description } };
        }
    }
}
