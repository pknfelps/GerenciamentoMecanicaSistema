using Domain.Interface.Stock;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto;
using Service.Interface.Dto.Stock;

namespace ServiceTests
{
    public class StockServiceTests
    {
        private IStockService Service { get; set; }
        private IStockRepository Repository { get; set; }

        private static CreatePartDto PartToRegister { get; } = new("Óleo de motor", "Lubrax", 41.90, 5);
        private static CreatePartDto PartToFailRegister { get; } = new("Teste", "Testando", 15, 1);

        private static readonly Guid ExistingPartId = Guid.NewGuid();
        private static IPart ExistingPart
        {
            get
            {
                var part = Substitute.For<IPart>();
                part.Id.Returns(ExistingPartId);
                part.Name.Returns("Vela de ignição");
                part.Brand.Returns("Bosch");
                part.Price.Returns(6.00);
                part.Amount.Returns(20);
                part.ReservedAmount.Returns(5);
                return part;
            }
        }

        private static readonly Guid ExistingPart2Id = Guid.NewGuid();
        private static IPart ExistingPart2
        {
            get
            {
                var part = Substitute.For<IPart>();
                part.Id.Returns(ExistingPart2Id);
                part.Name.Returns("Flúido para radiador");
                part.Brand.Returns("Gitanes");
                part.Price.Returns(30.00);
                part.Amount.Returns(5);
                part.ReservedAmount.Returns(0);
                return part;
            }
        }

        private static PartDto ExistingPartDto { get; } = new(ExistingPartId, "Vela de ignição", "Bosch", 6.00, 20, 5);
        private static PartDto ExistingPart2Dto { get; } = new(ExistingPart2Id, "Flúido para radiador", "Gitanes", 30.00, 5, 0);
        private static UpdateItemDto<int> PartToFailIntOperations { get; } = new(Guid.NewGuid(), 5);
        private static UpdateItemDto<double> PartToFailDoubleOperations { get; } = new(Guid.NewGuid(), 10.00);
        private static UpdateItemDto<int> PartToAddAmount { get; } = new(ExistingPartId, 5);
        private static UpdateItemDto<int> PartToFailAddAmount { get; } = new(ExistingPart2Id, 5);
        private static UpdateItemDto<double> PartToUpdatePrice { get; } = new(ExistingPartId, 10.00);
        private static UpdateItemDto<double> PartToFailUpdatePrice { get; } = new(ExistingPart2Id, 35.00);

        [SetUp]
        public void SetUp()
        {
            Repository = Substitute.For<IStockRepository>();

            Repository.RegisterNewPart(Arg.Any<IPart>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IPart>(0);

                if (item.Name == PartToRegister.Name && item.Brand == PartToRegister.Brand && item.Price == PartToRegister.Price && item.Amount == PartToRegister.Amount)
                    return 1;

                return 0;
            });

            List<IPart> parts = new List<IPart>() { ExistingPart, ExistingPart2 };
            Repository.GetParts().Returns(parts);

            Repository.GetPart(name: Arg.Any<string>(), brand: Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(1);
                var brand = callInfo.ArgAt<string>(2);

                return parts.FirstOrDefault(x => x.Name == name && x.Brand == brand);
            });

            Repository.GetPart(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                return parts.FirstOrDefault(x => x.Id == id);
            });

            Repository.UpdatePartAmount(Arg.Any<IPart>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IPart>(0);

                if (item.Name == ExistingPart.Name && item.Brand == ExistingPart.Brand)
                    return 1;

                return 0;
            });

            Repository.UpdatePartPrice(Arg.Any<IPart>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IPart>(0);

                if (item.Name == ExistingPart.Name && item.Brand == ExistingPart.Brand)
                    return 1;

                return 0;
            });

            Repository.DeletePart(Arg.Any<Guid>()).Returns(callInfo =>
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
            await Service.RegisterNewPart(PartToRegister);

            await Repository.Received(1).GetPart(name: PartToRegister.Name, brand: PartToRegister.Brand);
            await Repository.ReceivedWithAnyArgs(1).RegisterNewPart(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotRegisterNewPartIfAlreadyExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RegisterNewPart(ExistingPartDto));

            await Repository.Received(1).GetPart(name: ExistingPartDto.Name, brand: ExistingPartDto.Brand);
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToRegisterNewPart()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RegisterNewPart(PartToFailRegister));

            await Repository.Received(1).GetPart(name: PartToFailRegister.Name, brand: PartToFailRegister.Brand);
            await Repository.ReceivedWithAnyArgs(1).RegisterNewPart(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustGetParts()
        {
            var itens = (await Service.GetParts()).ToList();

            await Repository.Received(1).GetParts();

            Assert.That(itens, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(itens[0].Equals(ExistingPartDto), Is.True);
                Assert.That(itens[1].Equals(ExistingPart2Dto), Is.True);
            });
        }

        [Test]
        public async Task MustGetPartByNameAndBrand()
        {
            var item = await Service.GetPart(name: ExistingPart.Name, brand: ExistingPart.Brand);

            await Repository.Received(1).GetPart(name: ExistingPart.Name, brand: ExistingPart.Brand);

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Equals(ExistingPartDto), Is.True);
        }

        [Test]
        public async Task MustNotGetPartByNameAndBrandIfNotExists()
        {
            var item = await Service.GetPart(name: PartToRegister.Name, brand: PartToRegister.Brand);

            await Repository.Received(1).GetPart(name: PartToRegister.Name, brand: PartToRegister.Brand);

            Assert.That(item, Is.Null);
        }

        [Test]
        public async Task MustGetPartById()
        {
            var item = await Service.GetPart(ExistingPart.Id);

            await Repository.Received(1).GetPart(ExistingPart.Id);

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Equals(ExistingPartDto), Is.True);
        }

        [Test]
        public async Task MustNotGetPartByIdIfNotExists()
        {
            var item = await Service.GetPart(Guid.NewGuid());

            await Repository.Received(1).GetPart(Arg.Any<Guid>());

            Assert.That(item, Is.Null);
        }

        [Test]
        public async Task MustNotGetPartIfNoParameterWasGiven()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.GetPart());
        }

        [Test]
        public async Task MustAddPartAmount()
        {
            await Service.AddPartAmount(PartToAddAmount.Id, PartToAddAmount.Value);

            await Repository.Received(1).GetPart(PartToAddAmount.Id);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotAddPartAmountIfPartDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddPartAmount(PartToFailIntOperations.Id, PartToFailIntOperations.Value));

            await Repository.Received(1).GetPart(PartToFailIntOperations.Id);
            await Repository.Received(0).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustRemovePartAmount()
        {
            await Service.RemovePartAmount(PartToAddAmount.Id, PartToAddAmount.Value);

            await Repository.Received(1).GetPart(PartToAddAmount.Id);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotRemovePartAmountIfPartDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemovePartAmount(PartToFailIntOperations.Id, PartToFailIntOperations.Value));

            await Repository.Received(1).GetPart(PartToFailIntOperations.Id);
            await Repository.Received(0).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustReservePartAmount()
        {
            await Service.ReservePartAmount(PartToAddAmount.Id, PartToAddAmount.Value);

            await Repository.Received(1).GetPart(PartToAddAmount.Id);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotReservePartAmountIfPartDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ReservePartAmount(PartToFailIntOperations.Id, PartToFailIntOperations.Value));

            await Repository.Received(1).GetPart(PartToFailIntOperations.Id);
            await Repository.Received(0).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustRestorePartAmount()
        {
            await Service.RestorePartAmount(PartToAddAmount.Id, PartToAddAmount.Value);

            await Repository.Received(1).GetPart(PartToAddAmount.Id);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotRestorePartAmountIfPartDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RestorePartAmount(PartToFailIntOperations.Id, PartToFailIntOperations.Value));

            await Repository.Received(1).GetPart(PartToFailIntOperations.Id);
            await Repository.Received(0).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustConsumePartAmount()
        {
            await Service.ConsumeReservedAmount(PartToAddAmount.Id, PartToAddAmount.Value);

            await Repository.Received(1).GetPart(PartToAddAmount.Id);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotConsumePartAmountIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ConsumeReservedAmount(PartToFailIntOperations.Id, PartToFailIntOperations.Value));

            await Repository.Received(1).GetPart(PartToFailIntOperations.Id);
            await Repository.Received(0).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailUpdatePartAmount()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddPartAmount(PartToFailAddAmount.Id, PartToFailAddAmount.Value));

            await Repository.Received(1).GetPart(ExistingPart2Id);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustUpdatePartPrice()
        {
            await Service.UpdatePartPrice(PartToUpdatePrice.Id, PartToUpdatePrice.Value);

            await Repository.Received(1).GetPart(ExistingPartId);
            await Repository.Received(1).UpdatePartPrice(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotUpdatePartPriceIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.UpdatePartPrice(PartToFailDoubleOperations.Id, PartToFailDoubleOperations.Value));

            await Repository.Received(1).GetPart(PartToFailDoubleOperations.Id);
            await Repository.Received(0).UpdatePartPrice(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailtToUpdatePartPrice()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.UpdatePartPrice(PartToFailUpdatePrice.Id, PartToFailUpdatePrice.Value));

            await Repository.Received(1).GetPart(ExistingPart2Id);
            await Repository.Received(1).UpdatePartPrice(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustDeletePart()
        {
            await Service.DeletePart(ExistingPart.Id);

            await Repository.Received(1).GetPart(id: ExistingPart.Id);
            await Repository.Received(1).DeletePart(ExistingPart.Id);
        }

        [Test]
        public async Task MustNotDeletePartIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeletePart(Guid.NewGuid()));

            await Repository.Received(1).GetPart(Arg.Any<Guid>());
            await Repository.Received(0).DeletePart(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailToDeletePart()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeletePart(ExistingPart2.Id));

            await Repository.Received(1).GetPart(id: ExistingPart2.Id);
            await Repository.Received(1).DeletePart(ExistingPart2.Id);
        }
    }
}
