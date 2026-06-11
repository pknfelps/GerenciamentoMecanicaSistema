using Domain.Interface.Stock;
using Domain.Stock;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto.Stock;

namespace ServiceTests
{
    public class StockServiceTests
    {
        private IStockService Service { get; set; }
        private IStockRepository Repository { get; set; }

        private static readonly CreatePartDto PartToRegister = new("Óleo de motor", "Lubrax", 41.90, 5);
        private static readonly CreatePartDto PartToFailRegister = new("Teste", "Testando", 15, 1);
        private static readonly PartUpdateDto<int> PartToFailIntOperations = new(Guid.NewGuid(), 5);
        private static readonly PartUpdateDto<double> PartToFailDoubleOperations = new(Guid.NewGuid(), 10.00);
        private static List<IPart> StockParts;
        private static List<PartDto> StockPartsDtos;
        private static PartUpdateDto<int> PartToAddAmount;
        private static PartUpdateDto<int> PartToFailAddAmount;
        private static PartUpdateDto<double> PartToUpdatePrice;
        private static PartUpdateDto<double> PartToFailUpdatePrice;

        [SetUp]
        public void SetUp()
        {
            StockParts = 
            [ 
                new Part(Guid.NewGuid(), "Vela de ignição", "Bosch", 6.00, 20, 5),
                new Part(Guid.NewGuid(), "Flúido para radiador", "Gitanes", 30.00, 5, 0)
            ];

            StockPartsDtos =
            [
                new PartDto(Guid.NewGuid(), "Vela de ignição", "Bosch", 6.00, 20, 5),
                new PartDto(Guid.NewGuid(), "Flúido para radiador", "Gitanes", 30.00, 5, 0)
            ];

            PartToAddAmount = new(StockParts[0].Id, 5);
            PartToFailAddAmount = new(StockParts[1].Id, 5);
            PartToUpdatePrice = new(StockParts[0].Id, 10.00);
            PartToFailUpdatePrice = new(StockParts[1].Id, 35.00);

            Repository = Substitute.For<IStockRepository>();

            Repository.RegisterNewPart(Arg.Any<IPart>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IPart>(0);

                if (item.Name == PartToRegister.Name && item.Brand == PartToRegister.Brand && item.Price == PartToRegister.Price && item.Amount == PartToRegister.Amount)
                    return 1;

                return 0;
            });

            Repository.GetParts().Returns(StockParts);

            Repository.GetPart(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(0);
                var brand = callInfo.ArgAt<string>(1);

                return StockParts.FirstOrDefault(x => x.Name == name && x.Brand == brand);
            });

            Repository.UpdatePartAmount(Arg.Any<IPart>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IPart>(0);

                if (item.Name == StockParts[0].Name && item.Brand == StockParts[0].Brand)
                    return 1;

                return 0;
            });

            Repository.UpdatePartPrice(Arg.Any<IPart>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IPart>(0);

                if (item.Name == StockParts[0].Name && item.Brand == StockParts[0].Brand)
                    return 1;

                return 0;
            });

            Repository.DeletePart(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == StockParts[0].Id)
                    return 1;

                return 0;
            });

            Service = new StockService(Repository);
        }

        [Test]
        public async Task MustRegisterNewPart()
        {
            await Service.RegisterNewPart(PartToRegister);

            await Repository.Received(1).GetPart(PartToRegister.Name, PartToRegister.Brand);
            await Repository.ReceivedWithAnyArgs(1).RegisterNewPart(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotRegisterNewPartIfAlreadyExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RegisterNewPart(StockPartsDtos[0]));

            await Repository.Received(1).GetPart(StockPartsDtos[0].Name, StockPartsDtos[0].Brand);
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToRegisterNewPart()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RegisterNewPart(PartToFailRegister));

            await Repository.Received(1).GetPart(PartToFailRegister.Name, PartToFailRegister.Brand);
            await Repository.ReceivedWithAnyArgs(1).RegisterNewPart(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustAddPartAmount()
        {
            await Service.AddPartAmount(PartToAddAmount);

            await Repository.Received(1).GetPart(StockParts[0].Name, StockParts[0].Brand);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotAddPartAmountIfPartDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddPartAmount(PartToFailIntOperations));

            await Repository.Received(1).GetPart(PartToRegister.Name, PartToRegister.Brand);
            await Repository.Received(0).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustRemovePartAmount()
        {
            await Service.RemovePartAmount(PartToAddAmount);

            await Repository.Received(1).GetPart(StockParts[0].Name, StockParts[0].Brand);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotRemovePartAmountIfPartDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemovePartAmount(PartToFailIntOperations));

            await Repository.Received(1).GetPart(PartToRegister.Name, PartToRegister.Brand);
            await Repository.Received(0).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustReservePartAmount()
        {
            await Service.ReservePartAmount(PartToAddAmount);

            await Repository.Received(1).GetPart(StockParts[0].Name, StockParts[0].Brand);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotReservePartAmountIfPartDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ReservePartAmount(PartToFailIntOperations));

            await Repository.Received(1).GetPart(PartToRegister.Name, PartToRegister.Brand);
            await Repository.Received(0).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustRestorePartAmount()
        {
            await Service.RestorePartAmount(PartToAddAmount);

            await Repository.Received(1).GetPart(StockParts[0].Name, StockParts[0].Brand);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotRestorePartAmountIfPartDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RestorePartAmount(PartToFailIntOperations));

            await Repository.Received(1).GetPart(PartToRegister.Name, PartToRegister.Brand);
            await Repository.Received(0).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailUpdatePartAmount()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddPartAmount(PartToFailAddAmount));

            await Repository.Received(1).GetPart(StockParts[1].Name, StockParts[1].Brand);
            await Repository.Received(1).UpdatePartAmount(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustGetParts()
        {
            var itens = (await Service.GetParts()).ToList();

            await Repository.Received(1).GetParts();

            Assert.That(itens, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(itens[0].Equals(StockPartsDtos[0]), Is.True);
                Assert.That(itens[1].Equals(StockPartsDtos[1]), Is.True);
            });
        }

        [Test]
        public async Task MustGetPart()
        {
            var item = await Service.GetPart(StockParts[0].Name, StockParts[0].Brand);

            await Repository.Received(1).GetPart(StockParts[0].Name, StockParts[0].Brand);

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Equals(StockPartsDtos[0]), Is.True);
        }

        [Test]
        public async Task MustNotGetPartIfNotExists()
        {
            var item = await Service.GetPart(PartToRegister.Name, PartToRegister.Brand);

            await Repository.Received(1).GetPart(PartToRegister.Name, PartToRegister.Brand);

            Assert.That(item, Is.Null);
        }

        [Test]
        public async Task MustUpdatePartPrice()
        {
            await Service.UpdatePartPrice(PartToUpdatePrice);

            await Repository.Received(1).GetPart(StockParts[0].Name, StockParts[0].Brand);
            await Repository.Received(1).UpdatePartPrice(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustNotUpdatePartPriceIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.UpdatePartPrice(PartToFailDoubleOperations));

            await Repository.Received(1).GetPart(PartToRegister.Name, PartToRegister.Brand);
            await Repository.Received(0).UpdatePartPrice(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailtToUpdatePartPrice()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.UpdatePartPrice(PartToFailUpdatePrice));

            await Repository.Received(1).GetPart(StockParts[1].Name, StockParts[1].Brand);
            await Repository.Received(1).UpdatePartPrice(Arg.Any<IPart>());
        }

        [Test]
        public async Task MustDeletePart()
        {
            await Service.DeletePart(StockParts[0].Name, StockParts[0].Brand);

            await Repository.Received(1).GetPart(StockParts[0].Name, StockParts[0].Brand);
            await Repository.Received(1).DeletePart(StockParts[0].Id);
        }

        [Test]
        public async Task MustNotDeletePartIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeletePart(PartToRegister.Name, PartToRegister.Brand));

            await Repository.Received(1).GetPart(PartToRegister.Name, PartToRegister.Brand);
            await Repository.Received(0).DeletePart(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailToDeletePart()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeletePart(StockParts[1].Name, StockParts[1].Brand));

            await Repository.Received(1).GetPart(StockParts[1].Name, StockParts[1].Brand);
            await Repository.Received(1).DeletePart(StockParts[1].Id);
        }
    }
}
