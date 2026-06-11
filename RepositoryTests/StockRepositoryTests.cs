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

        private static readonly IPart PartToRegister = new Part("Óleo de motor", "Lubrax", 41.90, 5);

        private static readonly Guid ExistingPartId = Guid.NewGuid();

        private static readonly List<IPart> StockParts =
        [
            new Part(ExistingPartId, "Vela de ignição", "Bosch", 6.00, 20, 5),
            new Part(Guid.NewGuid(), "Flúido para radiador", "Gitanes", 30.00, 5, 0)
        ];

        private static readonly IPart PartToUpdatePrice = new Part(ExistingPartId, "Vela de ignição", "Bosch", 10.00, 20, 5);
        private static readonly IPart PartToUpdateAmount = new Part(ExistingPartId, "Vela de ignição", "Bosch", 6.00, 15, 10);

        protected override async Task InternalSetup()
        {
            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS stock (
                id UUID PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                brand VARCHAR(255) NOT NULL,
                price DOUBLE PRECISION NOT NULL,
                amount INT NOT NULL,
                reserved_amount INT NOT NULL DEFAULT 0);
                """);

            Repository = new StockRepository(Connection);

            foreach (var item in StockParts)
                await Repository.RegisterNewPart(item);
        }

        [Test]
        public async Task MustRegisterNewPart()
        {
            var registry = await Repository.RegisterNewPart(PartToRegister);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustGetItens()
        {
            var itens = (await Repository.GetParts()).ToList();

            Assert.That(itens, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(itens, Has.Count.EqualTo(2));

                Assert.That(itens[0], Is.Not.Null);
                Assert.That(itens[0].Name, Is.EqualTo(StockParts[0].Name));
                Assert.That(itens[0].Brand, Is.EqualTo(StockParts[0].Brand));
                Assert.That(itens[0].Price, Is.EqualTo(StockParts[0].Price));
                Assert.That(itens[0].Amount, Is.EqualTo(StockParts[0].Amount));
                Assert.That(itens[0].ReservedAmount, Is.EqualTo(StockParts[0].ReservedAmount));

                Assert.That(itens[1], Is.Not.Null);
                Assert.That(itens[1].Name, Is.EqualTo(StockParts[1].Name));
                Assert.That(itens[1].Brand, Is.EqualTo(StockParts[1].Brand));
                Assert.That(itens[1].Price, Is.EqualTo(StockParts[1].Price));
                Assert.That(itens[1].Amount, Is.EqualTo(StockParts[1].Amount));
                Assert.That(itens[1].ReservedAmount, Is.EqualTo(StockParts[1].ReservedAmount));
            });
        }

        [Test]
        public async Task MustGetPart()
        {
            var item = await Repository.GetPart(StockParts[0].Name, StockParts[0].Brand);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Name, Is.EqualTo(StockParts[0].Name));
                Assert.That(item.Brand, Is.EqualTo(StockParts[0].Brand));
                Assert.That(item.Price, Is.EqualTo(StockParts[0].Price));
                Assert.That(item.Amount, Is.EqualTo(StockParts[0].Amount));
                Assert.That(item.ReservedAmount, Is.EqualTo(StockParts[0].ReservedAmount));
            });
        }

        [Test]
        public async Task MustUpdatePartPrice()
        {
            var registry = await Repository.UpdatePartPrice(PartToUpdatePrice);

            Assert.That(registry, Is.Not.EqualTo(0));

            var item = await Repository.GetPart(PartToUpdatePrice.Name, PartToUpdatePrice.Brand);

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Price, Is.EqualTo(PartToUpdatePrice.Price));
        }

        [Test]
        public async Task MustUpdatePartAmount()
        {
            var registry = await Repository.UpdatePartAmount(PartToUpdateAmount);

            Assert.That(registry, Is.Not.EqualTo(0));

            var item = await Repository.GetPart(PartToUpdateAmount.Name, PartToUpdateAmount.Brand);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(PartToUpdateAmount.Amount));
                Assert.That(item.ReservedAmount, Is.EqualTo(PartToUpdateAmount.ReservedAmount));
            });
        }

        [Test]
        public async Task MustDeletePart()
        {
            var registry = await Repository.DeletePart(StockParts[0].Id);

            Assert.That(registry, Is.Not.EqualTo(0));
        }
    }
}
