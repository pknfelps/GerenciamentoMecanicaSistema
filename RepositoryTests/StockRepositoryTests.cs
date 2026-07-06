using Dapper;
using Domain.Interface.Stock;
using NSubstitute;
using Repository;
using Repository.Interface;

namespace RepositoryTests
{
    internal class StockRepositoryTests : BaseRepositoryTests
    {
        private IStockRepository Repository { get; set; }

        private static IMaterial PartToRegister
        {
            get
            {
                var part = Substitute.For<IMaterial>();
                part.Id.Returns(Guid.NewGuid());
                part.Name.Returns("Óleo de motor");
                part.Brand.Returns("Lubrax");
                part.Price.Returns(0);
                part.Amount.Returns(5);
                part.ReservedAmount.Returns(0);
                return part;
            }
        }

        private static readonly Guid ExistingPartId = Guid.NewGuid();
        private static IMaterial ExistingPart
        {
            get
            {
                var part = Substitute.For<IMaterial>();
                part.Id.Returns(ExistingPartId);
                part.Name.Returns("Vela de ignição");
                part.Brand.Returns("Bosch");
                part.Price.Returns(6.00);
                part.Amount.Returns(20);
                part.ReservedAmount.Returns(5);
                return part;
            }
        }

        private static IMaterial PartToUpdatePrice
        {
            get
            {
                var part = Substitute.For<IMaterial>();
                part.Id.Returns(ExistingPartId);
                part.Name.Returns("Vela de ignição");
                part.Brand.Returns("Bosch");
                part.Price.Returns(10.00);
                part.Amount.Returns(20);
                part.ReservedAmount.Returns(5);
                return part;
            }
        }

        private static IMaterial PartToUpdateAmount
        {
            get
            {
                var part = Substitute.For<IMaterial>();
                part.Id.Returns(ExistingPartId);
                part.Name.Returns("Vela de ignição");
                part.Brand.Returns("Bosch");
                part.Price.Returns(6.00);
                part.Amount.Returns(15);
                part.ReservedAmount.Returns(10);
                return part;
            }
        }

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

            await Repository.RegisterNewMaterial(ExistingPart);
        }

        [Test]
        public async Task MustRegisterNewPart()
        {
            var registry = await Repository.RegisterNewMaterial(PartToRegister);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustGetParts()
        {
            var itens = (await Repository.GetMaterials()).ToList();

            Assert.That(itens, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(itens, Has.Count.EqualTo(1));

                Assert.That(itens[0], Is.Not.Null);
                Assert.That(itens[0].Name, Is.EqualTo(ExistingPart.Name));
                Assert.That(itens[0].Brand, Is.EqualTo(ExistingPart.Brand));
                Assert.That(itens[0].Price, Is.EqualTo(ExistingPart.Price));
                Assert.That(itens[0].Amount, Is.EqualTo(ExistingPart.Amount));
                Assert.That(itens[0].ReservedAmount, Is.EqualTo(ExistingPart.ReservedAmount));
            });
        }

        [Test]
        public async Task MustGetPartByIdAndBrand()
        {
            var item = await Repository.GetMaterial(id: ExistingPart.Id);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Name, Is.EqualTo(ExistingPart.Name));
                Assert.That(item.Brand, Is.EqualTo(ExistingPart.Brand));
                Assert.That(item.Price, Is.EqualTo(ExistingPart.Price));
                Assert.That(item.Amount, Is.EqualTo(ExistingPart.Amount));
                Assert.That(item.ReservedAmount, Is.EqualTo(ExistingPart.ReservedAmount));
            });
        }

        [Test]
        public async Task MustNotGetPartByIdAndBrandIfNotExists()
        {
            var item = await Repository.GetMaterial(id: Guid.NewGuid());

            Assert.That(item, Is.Null);
        }

        [Test]
        public async Task MustGetPartByNameAndBrand()
        {
            var item = await Repository.GetMaterial(name: ExistingPart.Name, brand: ExistingPart.Brand);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Name, Is.EqualTo(ExistingPart.Name));
                Assert.That(item.Brand, Is.EqualTo(ExistingPart.Brand));
                Assert.That(item.Price, Is.EqualTo(ExistingPart.Price));
                Assert.That(item.Amount, Is.EqualTo(ExistingPart.Amount));
                Assert.That(item.ReservedAmount, Is.EqualTo(ExistingPart.ReservedAmount));
            });
        }

        [Test]
        public async Task MustNotGetPartByNameAndBrandIfNotExists()
        {
            var item = await Repository.GetMaterial(name: PartToRegister.Name, brand: PartToRegister.Brand);

            Assert.That(item, Is.Null);
        }

        [Test]
        public async Task MustGetPartById()
        {
            var item = await Repository.GetMaterial(ExistingPart.Id);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Name, Is.EqualTo(ExistingPart.Name));
                Assert.That(item.Brand, Is.EqualTo(ExistingPart.Brand));
                Assert.That(item.Price, Is.EqualTo(ExistingPart.Price));
                Assert.That(item.Amount, Is.EqualTo(ExistingPart.Amount));
                Assert.That(item.ReservedAmount, Is.EqualTo(ExistingPart.ReservedAmount));
            });
        }

        [Test]
        public async Task MustNotGetPartByIdIfNotExists()
        {
            var item = await Repository.GetMaterial(PartToRegister.Id);

            Assert.That(item, Is.Null);
        }

        [Test]
        public async Task MustUpdatePartPrice()
        {
            var registry = await Repository.UpdateMaterialPrice(PartToUpdatePrice);

            Assert.That(registry, Is.Not.EqualTo(0));

            var item = await Repository.GetMaterial(name: PartToUpdatePrice.Name, brand: PartToUpdatePrice.Brand);

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Price, Is.EqualTo(PartToUpdatePrice.Price));
        }

        [Test]
        public async Task MustUpdatePartAmount()
        {
            var registry = await Repository.UpdateMaterialAmount(PartToUpdateAmount);

            Assert.That(registry, Is.Not.EqualTo(0));

            var item = await Repository.GetMaterial(name: PartToUpdateAmount.Name, brand: PartToUpdateAmount.Brand);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(PartToUpdateAmount.Amount));
                Assert.That(item.ReservedAmount, Is.EqualTo(PartToUpdateAmount.ReservedAmount));
            });
        }

        [Test]
        public async Task MustRollbackMaterialAmountUpdateInTransaction()
        {
            var transactionContext = new DbTransactionContext();
            var transactionManager = new TransactionManager(Connection, transactionContext, Substitute.For<Microsoft.Extensions.Logging.ILogger<TransactionManager>>());
            var transactionalRepository = new StockRepository(Connection, transactionContext);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await transactionManager.ExecuteInTransaction(async () =>
                {
                    await transactionalRepository.UpdateMaterialAmount(PartToUpdateAmount);
                    throw new InvalidOperationException("force rollback");
                }));

            var item = await Repository.GetMaterial(id: ExistingPart.Id);

            Assert.That(item, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(item.Amount, Is.EqualTo(ExistingPart.Amount));
                Assert.That(item.ReservedAmount, Is.EqualTo(ExistingPart.ReservedAmount));
            });
        }

        [Test]
        public async Task MustDeletePart()
        {
            var registry = await Repository.DeleteMaterial(ExistingPart.Id);

            Assert.That(registry, Is.Not.EqualTo(0));
        }
    }
}
