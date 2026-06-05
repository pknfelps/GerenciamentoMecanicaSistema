using Dapper;
using Domain.Costumer;
using Domain.Interface.Costumer;
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
                INSERT INTO vehicles(id, brand, model, year, license_plate)
                VALUES (@Id, @Brand, @Model, @Year, @LicensePlate);
                """;

        public static string GetVehiclesSql { get; private set; } = """
                SELECT id, brand, model, year, license_plate
                FROM vehicles
                LIMIT 50;
                """;

        public static string GetVehicleSql { get; private set; } = """
                SELECT id, brand, model, year, license_plate
                FROM vehicles
                WHERE license_plate = @licensePlate;
                """;

        public static string UpdateVehicleSql { get; private set; } = $"""
                UPDATE vehicles
                SET brand = @Brand,
                    model = @Model,
                    year = @Year
                WHERE license_plate = @LicensePlate;
                """;

        public static string DeleteVehicleSql { get; private set; } = """
                DELETE FROM vehicles
                WHERE license_plate = @licensePlate;
                """;

        public async Task<int> RegisterVehicle(IVehicle vehicle)
        {
            return await Connection.ExecuteAsync(RegisterVehicleSql, ToDb(vehicle));
        }

        public async Task<IEnumerable<IVehicle>> GetVehicles()
        {
            var vehicles = await Connection.QueryAsync<VehicleDb>(GetVehiclesSql);

            return vehicles.Select(ToDomain);
        }

        public async Task<IVehicle?> GetVehicle(string licensePlate)
        {
            var vehicle = await Connection.QuerySingleOrDefaultAsync<VehicleDb?>(GetVehicleSql, new { licensePlate });

            if (vehicle == null)
                return null;

            return ToDomain(vehicle);
        }

        public async Task<int> UpdateVehicle(IVehicle vehicle)
        {
            return await Connection.ExecuteAsync(UpdateVehicleSql, ToDb(vehicle));
        }

        public async Task<int> DeleteVehicle(string licensePlate)
        {
            return await Connection.ExecuteAsync(DeleteVehicleSql, new { licensePlate });
        }

        private static VehicleDb ToDb(IVehicle vehicle) => new(vehicle.Id, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.LicensePlate.License);

        private static Vehicle ToDomain(VehicleDb vehicle) => new(vehicle.Id, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.LicensePlate);
    }
}
