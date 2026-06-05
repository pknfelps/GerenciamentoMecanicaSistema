using Dapper;
using Domain.Interface.Stock;
using Domain.Stock;
using Repository.Dto;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class StockRepository(IDbConnection connection) : BaseRepository(connection), IStockRepository
    {
        public static string RegisterItemSql { get; private set; } = """
                INSERT INTO stock(item_name, brand, price, amount, reserved_amount)
                VALUES (@Name, @Brand, @Price, @Amount, @ReservedAmount);
                """;

        public static string GetItensSql { get; private set; } = """
                SELECT item_name, brand, price, amount, reserved_amount
                FROM stock
                LIMIT 50;
                """;

        public static string GetItemSql { get; private set; } = """
                SELECT item_name, brand, price, amount, reserved_amount
                FROM stock
                WHERE item_name = @Name AND brand = @Brand;
                """;

        public static string UpdateItemPriceSql { get; private set; } = """
                UPDATE stock
                SET price = @Price
                WHERE item_name = @Name AND brand = @Brand;
                """;

        public static string UpdateItemAmountSql { get; private set; } = """
                UPDATE stock
                SET amount = @Amount,
                    reserved_amount = @ReservedAmount
                WHERE item_name = @Name AND brand = @Brand;
                """;

        public static string DeleItemSql { get; private set; } = """
                DELETE FROM stock
                WHERE item_name = @Name AND brand = @Brand;
                """;

        public async Task<int> RegisterNewItem(IStockItem item)
        {
            return await Connection.ExecuteAsync(RegisterItemSql, item);
        }

        public async Task<IEnumerable<IStockItem?>> GetItens()
        {
            var itens = await Connection.QueryAsync<StockItemDb>(GetItensSql);

            return itens.Select(ToDomain);
        }

        public async Task<IStockItem?> GetItem(string name, string brand)
        {
            var item = await Connection.QuerySingleOrDefaultAsync<StockItemDb>(GetItemSql, new { Name = name, Brand = brand });

            if (item == null)
                return null;

            return ToDomain(item);
        }

        public async Task<int> UpdateItemPrice(IStockItem item)
        {
            return await Connection.ExecuteAsync(UpdateItemPriceSql, item);
        }

        public async Task<int> UpdateItemAmount(IStockItem item)
        {
            return await Connection.ExecuteAsync(UpdateItemAmountSql, item);
        }

        public async Task<int> DeleteItem(string name, string brand)
        {
            return await Connection.ExecuteAsync(DeleItemSql, new { Name = name, Brand = brand });
        }

        private static StockItem ToDomain(StockItemDb item) => new(item.Name, item.Brand, item.Price, item.Amount, item.ReservedAmount);
    }
}
