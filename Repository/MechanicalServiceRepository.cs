using Dapper;
using Domain.Interface.Service;
using Repository.Dto;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class MechanicalServiceRepository(IDbConnection connection) : BaseRepository(connection), IMechanicalServiceRepository
    {
        public static string RegisterServiceSql { get; private set; } = """
            INSERT INTO services(id, description, hours, price_per_hour, amount)
            VALUES (@Id, @Description, @Hours, @Price_Per_Hour, @Amount);
            """;

        public static string GetServicesSql { get; private set; } = """
            SELECT id, description, hours, price_per_hour, amount
            FROM services;
            """;

        public static string GetServiceByIdSql { get; private set; } = """
            SELECT id, description, hours, price_per_hour, amount
            FROM services
            WHERE id = @Id;
            """;

        public static string GetServiceByDescriptionSql { get; private set; } = """
            SELECT id, description, hours, price_per_hour, amount
            FROM services
            WHERE description = @Description;
            """;

        public static string UpdateServiceSql { get; private set; } = """
            UPDATE services
            SET description = @Description,
                hours = @Hours,
                price_per_hour = @Price_Per_Hour
            Where id = @Id;
            """;

        public static string DeleteServiceSql { get; private set; } = """
            DELETE FROM services
            Where id = @Id;
            """;

        public async Task<int> RegisterService(IMechanicalService service)
        {
            return await Connection.ExecuteAsync(RegisterServiceSql, MechanicalServiceDb.Create(service));
        }

        public async Task<IEnumerable<IMechanicalService?>> GetServices()
        {
            var services = await Connection.QueryAsync<MechanicalServiceDb>(GetServicesSql);

            return services.Select(service => service.ToDomain());
        }

        public async Task<IMechanicalService?> GetService(Guid serviceId)
        {
            var service = await Connection.QuerySingleOrDefaultAsync<MechanicalServiceDb>(GetServiceByIdSql, new { Id = serviceId });

            if (service == null)
                return null;

            return service.ToDomain();
        }

        public async Task<IMechanicalService?> GetService(string description)
        {
            var service = await Connection.QuerySingleOrDefaultAsync<MechanicalServiceDb>(GetServiceByDescriptionSql, new { Description = description });

            if (service == null)
                return null;

            return service.ToDomain();
        }

        public async Task<int> UpdateService(IMechanicalService service)
        {
            return await Connection.ExecuteAsync(UpdateServiceSql, MechanicalServiceDb.Create(service));
        }

        public async Task<int> DeleteService(Guid serviceId)
        {
            return await Connection.ExecuteAsync(DeleteServiceSql, new { Id = serviceId });
        }
    }
}
