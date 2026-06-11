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
                LIMIT 50;
                """;

        public static string GetPartByNameAndBrandSql { get; private set; } = """
                SELECT id, name, brand, price, amount, reserved_amount
                FROM stock
                WHERE name = @Name AND brand = @Brand;
                """;

        public static string GetPartByIdSql { get; private set; } = """
                SELECT id, name, brand, price, amount, reserved_amount
                FROM stock
                WHERE id = @partId;
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

        public async Task<IEnumerable<IPart?>> GetParts()
        {
            var parts = await Connection.QueryAsync<PartDb>(GetItensSql);

            return parts.Select(part => part.ToDomain());
        }

        public async Task<IPart?> GetPart(string name, string brand)
        {
            var part = await Connection.QuerySingleOrDefaultAsync<PartDb>(GetPartByNameAndBrandSql, new { Name = name, Brand = brand });

            if (part == null)
                return null;

            return part.ToDomain();
        }

        public async Task<IPart?> GetPart(Guid partId)
        {
            var part = await Connection.QuerySingleOrDefaultAsync<PartDb>(GetPartByIdSql, new { partId });

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
    }
}
