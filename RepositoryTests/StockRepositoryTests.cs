using Dapper;
using Domain.Interface.Stock;
using Domain.Stock;
using Repository;
using Repository.Interface;

namespace RepositoryTests
{
    internal class StockRepositoryTests : BaseRepositoryTests
    {
        private IStockRepository Repository { get; set; }

        private static readonly IStockItem ItemToRegister = new StockItem("Óleo de motor", "Lubrax", 41.90, 5);

        private static readonly List<IStockItem> StockItems =
        [
            new StockItem("Vela de ignição", "Bosch", 6.00, 20, 5),
            new StockItem("Flúido para radiador", "Gitanes", 30.00, 5, 0)
        ];

        private static readonly IStockItem ItemToUpdatePrice = new StockItem("Vela de ignição", "Bosch", 10.00, 20, 5);
        private static readonly IStockItem ItemToUpdateAmount = new StockItem("Vela de ignição", "Bosch", 6.00, 15, 10);

        protected override async Task InternalSetup()
        {
            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS stock (
                id SERIAL PRIMARY KEY NOT NULL,
                item_name TEXT NOT NULL,
                brand TEXT NOT NULL,
                price DOUBLE PRECISION NOT NULL,
                amount INT NOT NULL,
                reserved_amount INT NOT NULL);
                """);

            Repository = new StockRepository(Connection);

            foreach (var item in StockItems)
                await Repository.RegisterNewItem(item);
        }

        [Test]
        public async Task MustRegisterNewItem()
        {
            var registry = await Repository.RegisterNewItem(ItemToRegister);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustGetItens()
        {
            var itens = (await Repository.GetItens()).ToList();

            Assert.That(itens, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(itens, Has.Count.EqualTo(2));

                Assert.That(itens[0], Is.Not.Null);
                Assert.That(itens[0].Name, Is.EqualTo(StockItems[0].Name));
                Assert.That(itens[0].Brand, Is.EqualTo(StockItems[0].Brand));
                Assert.That(itens[0].Price, Is.EqualTo(StockItems[0].Price));
                Assert.That(itens[0].Amount, Is.EqualTo(StockItems[0].Amount));
                Assert.That(itens[0].ReservedAmount, Is.EqualTo(StockItems[0].ReservedAmount));

                Assert.That(itens[1], Is.Not.Null);
                Assert.That(itens[1].Name, Is.EqualTo(StockItems[1].Name));
                Assert.That(itens[1].Brand, Is.EqualTo(StockItems[1].Brand));
                Assert.That(itens[1].Price, Is.EqualTo(StockItems[1].Price));
                Assert.That(itens[1].Amount, Is.EqualTo(StockItems[1].Amount));
                Assert.That(itens[1].ReservedAmount, Is.EqualTo(StockItems[1].ReservedAmount));
            });
        }

        [Test]
        public async Task MustGetItem()
        {
            var item = await Repository.GetItem(StockItems[0].Name, StockItems[0].Brand);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Name, Is.EqualTo(StockItems[0].Name));
                Assert.That(item.Brand, Is.EqualTo(StockItems[0].Brand));
                Assert.That(item.Price, Is.EqualTo(StockItems[0].Price));
                Assert.That(item.Amount, Is.EqualTo(StockItems[0].Amount));
                Assert.That(item.ReservedAmount, Is.EqualTo(StockItems[0].ReservedAmount));
            });
        }

        [Test]
        public async Task MustUpdateItemPrice()
        {
            var registry = await Repository.UpdateItemPrice(ItemToUpdatePrice);

            Assert.That(registry, Is.Not.EqualTo(0));

            var item = await Repository.GetItem(ItemToUpdatePrice.Name, ItemToUpdatePrice.Brand);

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Price, Is.EqualTo(ItemToUpdatePrice.Price));
        }

        [Test]
        public async Task MustUpdateItemAmount()
        {
            var registry = await Repository.UpdateItemAmount(ItemToUpdateAmount);

            Assert.That(registry, Is.Not.EqualTo(0));

            var item = await Repository.GetItem(ItemToUpdateAmount.Name, ItemToUpdateAmount.Brand);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(ItemToUpdateAmount.Amount));
                Assert.That(item.ReservedAmount, Is.EqualTo(ItemToUpdateAmount.ReservedAmount));
            });
        }

        [Test]
        public async Task MustDeleteItem()
        {
            var registry = await Repository.DeleteItem(StockItems[0].Name, StockItems[0].Brand);

            Assert.That(registry, Is.Not.EqualTo(0));
        }
    }
}
