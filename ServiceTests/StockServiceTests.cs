using Domain.Interface.Stock;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Stock;
using Service.Interface.Results.Stock;

namespace ServiceTests
{
    public class StockServiceTests
    {
        private IStockService Service { get; set; }
        private IStockRepository Repository { get; set; }

        private static CreateMaterialCommand PartToRegister { get; } = new("Óleo de motor", "Lubrax", 41.90m, 5);
        private static CreateMaterialCommand PartToFailRegister { get; } = new("Teste", "Testando", 15, 1);

        private static readonly Guid ExistingPartId = Guid.NewGuid();
        private static IMaterial ExistingPart
        {
            get
            {
                var part = Substitute.For<IMaterial>();
                part.Id.Returns(ExistingPartId);
                part.Name.Returns("Vela de ignição");
                part.Brand.Returns("Bosch");
                part.Price.Returns(6.00m);
                part.Amount.Returns(20);
                part.ReservedAmount.Returns(5);
                return part;
            }
        }

        private static readonly Guid ExistingPart2Id = Guid.NewGuid();
        private static IMaterial ExistingPart2
        {
            get
            {
                var part = Substitute.For<IMaterial>();
                part.Id.Returns(ExistingPart2Id);
                part.Name.Returns("Flúido para radiador");
                part.Brand.Returns("Gitanes");
                part.Price.Returns(30.00m);
                part.Amount.Returns(5);
                part.ReservedAmount.Returns(0);
                return part;
            }
        }

        private static MaterialResult ExistingPartResult { get; } = new(ExistingPartId, "Vela de ignição", "Bosch", 6.00m, 20, 5);
        private static MaterialResult ExistingPart2Result { get; } = new(ExistingPart2Id, "Flúido para radiador", "Gitanes", 30.00m, 5, 0);
        private static CreateMaterialCommand ExistingPartCommand { get; } = new("Vela de ignição", "Bosch", 6.00m, 20);
        private static (Guid Id, int Value) PartToFailIntOperations { get; } = new(Guid.NewGuid(), 5);
        private static (Guid Id, decimal Value) PartToFailDecimalOperations { get; } = new(Guid.NewGuid(), 10.00m);
        private static (Guid Id, int Value) PartToAddAmount { get; } = new(ExistingPartId, 5);
        private static (Guid Id, int Value) PartToFailAddAmount { get; } = new(ExistingPart2Id, 5);
        private static (Guid Id, decimal Value) PartToUpdatePrice { get; } = new(ExistingPartId, 10.00m);
        private static (Guid Id, decimal Value) PartToFailUpdatePrice { get; } = new(ExistingPart2Id, 35.00m);

        [SetUp]
        public void SetUp()
        {
            Repository = Substitute.For<IStockRepository>();

            Repository.RegisterNewMaterial(Arg.Any<IMaterial>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IMaterial>(0);

                if (item.Name == PartToRegister.Name && item.Brand == PartToRegister.Brand && item.Price == PartToRegister.Price && item.Amount == PartToRegister.Amount)
                    return 1;

                return 0;
            });

            List<IMaterial> parts = new List<IMaterial>() { ExistingPart, ExistingPart2 };
            Repository.GetMaterials().Returns(parts);

            Repository.GetMaterial(name: Arg.Any<string>(), brand: Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(1);
                var brand = callInfo.ArgAt<string>(2);

                return parts.FirstOrDefault(x => x.Name == name && x.Brand == brand);
            });

            Repository.GetMaterial(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                return parts.FirstOrDefault(x => x.Id == id);
            });

            Repository.UpdateMaterialAmount(Arg.Any<IMaterial>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IMaterial>(0);

                if (item.Name == ExistingPart.Name && item.Brand == ExistingPart.Brand)
                    return 1;

                return 0;
            });

            Repository.UpdateMaterialPrice(Arg.Any<IMaterial>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IMaterial>(0);

                if (item.Name == ExistingPart.Name && item.Brand == ExistingPart.Brand)
                    return 1;

                return 0;
            });

            Repository.DeleteMaterial(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingPart.Id)
                    return 1;

                return 0;
            });

            Service = new StockService(Repository);
        }

        [Test]
        public async Task MustRegisterNewPart()
        {
            await Service.RegisterNewMaterial(PartToRegister);

            await Repository.Received(1).GetMaterial(name: PartToRegister.Name, brand: PartToRegister.Brand);
            await Repository.ReceivedWithAnyArgs(1).RegisterNewMaterial(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustNotRegisterNewPartIfAlreadyExists()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.RegisterNewMaterial(ExistingPartCommand));

            await Repository.Received(1).GetMaterial(name: ExistingPartCommand.Name, brand: ExistingPartCommand.Brand);
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToRegisterNewPart()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.RegisterNewMaterial(PartToFailRegister));

            await Repository.Received(1).GetMaterial(name: PartToFailRegister.Name, brand: PartToFailRegister.Brand);
            await Repository.ReceivedWithAnyArgs(1).RegisterNewMaterial(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustGetParts()
        {
            var itens = (await Service.GetMaterials()).ToList();

            await Repository.Received(1).GetMaterials();

            Assert.That(itens, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(itens[0], Is.EqualTo(ExistingPartResult));
                Assert.That(itens[1], Is.EqualTo(ExistingPart2Result));
            });
        }

        [Test]
        public async Task MustGetPartByNameAndBrand()
        {
            var item = await Service.GetMaterial(name: ExistingPart.Name, brand: ExistingPart.Brand);

            await Repository.Received(1).GetMaterial(name: ExistingPart.Name, brand: ExistingPart.Brand);

            Assert.That(item, Is.EqualTo(ExistingPartResult));
        }

        [Test]
        public async Task MustNotGetPartByNameAndBrandIfNotExists()
        {
            var item = await Service.GetMaterial(name: PartToRegister.Name, brand: PartToRegister.Brand);

            await Repository.Received(1).GetMaterial(name: PartToRegister.Name, brand: PartToRegister.Brand);

            Assert.That(item, Is.Null);
        }

        [Test]
        public async Task MustGetPartById()
        {
            var item = await Service.GetMaterial(ExistingPart.Id);

            await Repository.Received(1).GetMaterial(ExistingPart.Id);

            Assert.That(item, Is.EqualTo(ExistingPartResult));
        }

        [Test]
        public async Task MustNotGetPartByIdIfNotExists()
        {
            var item = await Service.GetMaterial(Guid.NewGuid());

            await Repository.Received(1).GetMaterial(Arg.Any<Guid>());

            Assert.That(item, Is.Null);
        }

        [Test]
        public async Task MustNotGetPartIfNoParameterWasGiven()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.GetMaterial());
        }

        [Test]
        public async Task MustAddPartAmount()
        {
            await Service.AddMaterialAmount(PartToAddAmount.Id, PartToAddAmount.Value);

            await Repository.Received(1).GetMaterial(PartToAddAmount.Id);
            await Repository.Received(1).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustNotAddPartAmountIfPartDoentExist()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.AddMaterialAmount(PartToFailIntOperations.Id, PartToFailIntOperations.Value));

            await Repository.Received(1).GetMaterial(PartToFailIntOperations.Id);
            await Repository.Received(0).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustRemovePartAmount()
        {
            await Service.RemoveMaterialAmount(PartToAddAmount.Id, PartToAddAmount.Value);

            await Repository.Received(1).GetMaterial(PartToAddAmount.Id);
            await Repository.Received(1).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustNotRemovePartAmountIfPartDoentExist()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.RemoveMaterialAmount(PartToFailIntOperations.Id, PartToFailIntOperations.Value));

            await Repository.Received(1).GetMaterial(PartToFailIntOperations.Id);
            await Repository.Received(0).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustReservePartAmount()
        {
            await Service.ReserveMaterialAmount(PartToAddAmount.Id, PartToAddAmount.Value);

            await Repository.Received(1).GetMaterial(PartToAddAmount.Id);
            await Repository.Received(1).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustNotReservePartAmountIfPartDoentExist()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.ReserveMaterialAmount(PartToFailIntOperations.Id, PartToFailIntOperations.Value));

            await Repository.Received(1).GetMaterial(PartToFailIntOperations.Id);
            await Repository.Received(0).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustRestorePartAmount()
        {
            await Service.RestoreMaterialAmount(PartToAddAmount.Id, PartToAddAmount.Value);

            await Repository.Received(1).GetMaterial(PartToAddAmount.Id);
            await Repository.Received(1).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustNotRestorePartAmountIfPartDoentExist()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.RestoreMaterialAmount(PartToFailIntOperations.Id, PartToFailIntOperations.Value));

            await Repository.Received(1).GetMaterial(PartToFailIntOperations.Id);
            await Repository.Received(0).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustConsumePartAmount()
        {
            await Service.ConsumeReservedAmount(PartToAddAmount.Id, PartToAddAmount.Value);

            await Repository.Received(1).GetMaterial(PartToAddAmount.Id);
            await Repository.Received(1).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustNotConsumePartAmountIfNotExists()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.ConsumeReservedAmount(PartToFailIntOperations.Id, PartToFailIntOperations.Value));

            await Repository.Received(1).GetMaterial(PartToFailIntOperations.Id);
            await Repository.Received(0).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailUpdatePartAmount()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.AddMaterialAmount(PartToFailAddAmount.Id, PartToFailAddAmount.Value));

            await Repository.Received(1).GetMaterial(ExistingPart2Id);
            await Repository.Received(1).UpdateMaterialAmount(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustUpdatePartPrice()
        {
            await Service.UpdateMaterialPrice(PartToUpdatePrice.Id, PartToUpdatePrice.Value);

            await Repository.Received(1).GetMaterial(ExistingPartId);
            await Repository.Received(1).UpdateMaterialPrice(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustNotUpdatePartPriceIfNotExists()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.UpdateMaterialPrice(PartToFailDecimalOperations.Id, PartToFailDecimalOperations.Value));

            await Repository.Received(1).GetMaterial(PartToFailDecimalOperations.Id);
            await Repository.Received(0).UpdateMaterialPrice(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailtToUpdatePartPrice()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.UpdateMaterialPrice(PartToFailUpdatePrice.Id, PartToFailUpdatePrice.Value));

            await Repository.Received(1).GetMaterial(ExistingPart2Id);
            await Repository.Received(1).UpdateMaterialPrice(Arg.Any<IMaterial>());
        }

        [Test]
        public async Task MustDeletePart()
        {
            await Service.DeleteMaterial(ExistingPart.Id);

            await Repository.Received(1).GetMaterial(id: ExistingPart.Id);
            await Repository.Received(1).DeleteMaterial(ExistingPart.Id);
        }

        [Test]
        public async Task MustNotDeletePartIfNotExists()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.DeleteMaterial(Guid.NewGuid()));

            await Repository.Received(1).GetMaterial(Arg.Any<Guid>());
            await Repository.Received(0).DeleteMaterial(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailToDeletePart()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await Service.DeleteMaterial(ExistingPart2.Id));

            await Repository.Received(1).GetMaterial(id: ExistingPart2.Id);
            await Repository.Received(1).DeleteMaterial(ExistingPart2.Id);
        }
    }
}

