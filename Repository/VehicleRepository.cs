using Dapper;
using Domain.Customer;
using Domain.Interface.Vehicle;
using Domain.Vehicle;
using Repository.Dto;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class VehicleRepository(IDbConnection connection) : BaseRepository(connection), IVehicleRepository
    {
        public static string RegisterVehicleSql { get; private set; } = """
                INSERT INTO vehicles(id, customer_document, brand, model, year, license_plate)
                VALUES (@Id, @Customer_Document, @Brand, @Model, @Year, @License_Plate);
                """;

        public static string GetVehiclesSql { get; private set; } = """
                SELECT id, customer_document, brand, model, year, license_plate
                FROM vehicles
                {0}
                LIMIT 50;
                """;

        public static string UpdateVehicleSql { get; private set; } = $"""
                UPDATE vehicles
                SET brand = @Brand,
                    model = @Model,
                    year = @Year
                WHERE license_plate = @License_Plate;
                """;

        public static string DeleteVehicleSql { get; private set; } = """
                DELETE FROM vehicles
                WHERE id = @vehicleId;
                """;

        public async Task<int> RegisterVehicle(IVehicle vehicle)
        {
            return await Connection.ExecuteAsync(RegisterVehicleSql, VehicleDb.Create(vehicle));
        }

        public async Task<IEnumerable<IVehicle>> GetVehicles(Guid? id = null, string license_plate = "")
        {
            var vehicles = await Connection.QueryAsync<VehicleDb>(GetVehiclesSql.BuildQuery(BuildQueryParameters(id ,license_plate)));

            return vehicles.Select(vehicle => vehicle.ToDomain());
        }

        public async Task<IVehicle?> GetVehicle(Guid? id = null, string license_plate = "")
        {
            var vehicle = await Connection.QuerySingleOrDefaultAsync<VehicleDb?>(GetVehiclesSql.BuildQuery(BuildQueryParameters(id, license_plate)));

            if (vehicle == null)
                return null;

            return vehicle.ToDomain();
        }

        public async Task<int> UpdateVehicle(IVehicle vehicle)
        {
            return await Connection.ExecuteAsync(UpdateVehicleSql, VehicleDb.Create(vehicle));
        }

        public async Task<int> DeleteVehicle(Guid vehicleId)
        {
            return await Connection.ExecuteAsync(DeleteVehicleSql, new { vehicleId });
        }

        private static Dictionary<string, object?> BuildQueryParameters(Guid? id = null, string license_plate = "")
        {
            return new() { { nameof(id), id }, { nameof(license_plate), license_plate } };
        }
    }
}
