using Dapper;
using Domain.Interface.Stock;
using Repository.Dto;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class StockRepository(IDbConnection connection) : BaseRepository(connection), IStockRepository
    {
        public static string RegisterPartSql { get; private set; } = """
                INSERT INTO stock(id, name, brand, price, amount, reserved_amount)
                VALUES (@Id, @Name, @Brand, @Price, @Amount, @Reserved_Amount);
                """;

        public static string GetItensSql { get; private set; } = """
                SELECT id, name, brand, price, amount, reserved_amount
                FROM stock
                {0}
                LIMIT 50;
                """;

        public static string UpdatePartPriceSql { get; private set; } = """
                UPDATE stock
                SET price = @Price
                WHERE id = @Id;
                """;

        public static string UpdatePartAmountSql { get; private set; } = """
                UPDATE stock
                SET amount = @Amount,
                    reserved_amount = @Reserved_Amount
                WHERE id = @Id;
                """;

        public static string DelePartSql { get; private set; } = """
                DELETE FROM stock
                WHERE id = @Id;
                """;

        public async Task<int> RegisterNewPart(IPart part)
        {
            return await Connection.ExecuteAsync(RegisterPartSql, PartDb.Create(part));
        }

        public async Task<IEnumerable<IPart>> GetParts(Guid? id = null, string name = "", string brand = "")
        {
            var parts = await Connection.QueryAsync<PartDb>(GetItensSql.BuildQuery(BuildQueryParameters(id, name, brand)));

            return parts.Select(part => part.ToDomain());
        }

        public async Task<IPart?> GetPart(Guid? id = null, string name = "", string brand = "")
        {
            var part = await Connection.QuerySingleOrDefaultAsync<PartDb>(GetItensSql.BuildQuery(BuildQueryParameters(id, name, brand)));

            if (part == null)
                return null;

            return part.ToDomain();
        }

        public async Task<int> UpdatePartPrice(IPart part)
        {
            return await Connection.ExecuteAsync(UpdatePartPriceSql, PartDb.Create(part));
        }

        public async Task<int> UpdatePartAmount(IPart part)
        {
            return await Connection.ExecuteAsync(UpdatePartAmountSql, PartDb.Create(part));
        }

        public async Task<int> DeletePart(Guid partId)
        {
            return await Connection.ExecuteAsync(DelePartSql, new { Id = partId });
        }

        private static Dictionary<string, object?> BuildQueryParameters(Guid? id = null, string name = "", string brand = "")
        {
            return new() { { nameof(id), id }, { nameof(name), name }, { nameof(brand), brand } };
        }
    }
}
