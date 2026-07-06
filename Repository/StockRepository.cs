using Dapper;
using Domain.Interface.Stock;
using Repository.PersistenceModels;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class StockRepository(IDbConnection connection) : BaseRepository(connection), IStockRepository
    {
        public static string RegisterMaterialSql { get; private set; } = """
                INSERT INTO stock(id, name, brand, price, amount, reserved_amount)
                VALUES (@Id, @Name, @Brand, @Price, @Amount, @ReservedAmount);
                """;

        public static string GetItensSql { get; private set; } = """
                SELECT id, name, brand, price, amount, reserved_amount AS ReservedAmount
                FROM stock
                {0}
                LIMIT 50;
                """;

        public static string UpdateMaterialPriceSql { get; private set; } = """
                UPDATE stock
                SET price = @Price
                WHERE id = @Id;
                """;

        public static string UpdateMaterialAmountSql { get; private set; } = """
                UPDATE stock
                SET amount = @Amount,
                    reserved_amount = @ReservedAmount
                WHERE id = @Id;
                """;

        public static string DeleMaterialSql { get; private set; } = """
                DELETE FROM stock
                WHERE id = @Id;
                """;

        public async Task<int> RegisterNewMaterial(IMaterial material)
        {
            return await Connection.ExecuteAsync(RegisterMaterialSql, MaterialDb.Create(material));
        }

        public async Task<IEnumerable<IMaterial>> GetMaterials(Guid? id = null, string name = "", string brand = "")
        {
            var query = GetItensSql.BuildQuery(BuildQueryParameters(id, name, brand));
            var materials = await Connection.QueryAsync<MaterialDb>(query.Sql, query.Parameters);

            return materials.Select(material => material.ToDomain());
        }

        public async Task<IMaterial?> GetMaterial(Guid? id = null, string name = "", string brand = "")
        {
            var query = GetItensSql.BuildQuery(BuildQueryParameters(id, name, brand));
            var material = await Connection.QuerySingleOrDefaultAsync<MaterialDb>(query.Sql, query.Parameters);

            if (material == null)
                return null;

            return material.ToDomain();
        }

        public async Task<int> UpdateMaterialPrice(IMaterial material)
        {
            return await Connection.ExecuteAsync(UpdateMaterialPriceSql, MaterialDb.Create(material));
        }

        public async Task<int> UpdateMaterialAmount(IMaterial material)
        {
            return await Connection.ExecuteAsync(UpdateMaterialAmountSql, MaterialDb.Create(material));
        }

        public async Task<int> DeleteMaterial(Guid materialId)
        {
            return await Connection.ExecuteAsync(DeleMaterialSql, new { Id = materialId });
        }

        private static Dictionary<string, object?> BuildQueryParameters(Guid? id = null, string name = "", string brand = "")
        {
            return new() { { nameof(id), id }, { nameof(name), name }, { nameof(brand), brand } };
        }
    }
}
