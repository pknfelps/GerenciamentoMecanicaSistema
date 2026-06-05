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

        private static readonly StockItemDto ItemToRegister = new("Óleo de motor", "Lubrax", 41.90, 5, 0);
        private static readonly StockItemDto ItemToFailRegister = new("Teste", "Testando", 15, 1, 0);
        private static readonly StockItemUpdateDto<int> ItemToFailIntOperations = new(ItemToRegister.Name, ItemToRegister.Brand, 5);
        private static readonly StockItemUpdateDto<double> ItemToFailDoubleOperations = new(ItemToRegister.Name, ItemToRegister.Brand, 10.00);
        private static List<IStockItem> StockItems;
        private static List<StockItemDto> StockItemsDtos;
        private static StockItemUpdateDto<int> ItemToAddAmount;
        private static StockItemUpdateDto<int> ItemToFailAddAmount;
        private static StockItemUpdateDto<double> ItemToUpdatePrice;
        private static StockItemUpdateDto<double> ItemToFailUpdatePrice;

        [SetUp]
        public void SetUp()
        {
            StockItems = 
            [ 
                new StockItem("Vela de ignição", "Bosch", 6.00, 20, 5),
                new StockItem("Flúido para radiador", "Gitanes", 30.00, 5, 0)
            ];

            StockItemsDtos =
            [
                new StockItemDto("Vela de ignição", "Bosch", 6.00, 20, 5),
                new StockItemDto("Flúido para radiador", "Gitanes", 30.00, 5, 0)
            ];

            ItemToAddAmount = new(StockItems[0].Name, StockItems[0].Brand, 5);
            ItemToFailAddAmount = new(StockItems[1].Name, StockItems[1].Brand, 5);
            ItemToUpdatePrice = new(StockItems[0].Name, StockItems[0].Brand, 10.00);
            ItemToFailUpdatePrice = new(StockItems[1].Name, StockItems[1].Brand, 35.00);

            Repository = Substitute.For<IStockRepository>();

            Repository.RegisterNewItem(Arg.Any<IStockItem>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IStockItem>(0);

                if (item.Name == ItemToRegister.Name && item.Brand == ItemToRegister.Brand && item.Price == ItemToRegister.Price && item.Amount == ItemToRegister.Amount)
                    return 1;

                return 0;
            });

            Repository.GetItens().Returns(StockItems);

            Repository.GetItem(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(0);
                var brand = callInfo.ArgAt<string>(1);

                return StockItems.FirstOrDefault(x => x.Name == name && x.Brand == brand);
            });

            Repository.UpdateItemAmount(Arg.Any<IStockItem>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IStockItem>(0);

                if (item.Name == StockItems[0].Name && item.Brand == StockItems[0].Brand)
                    return 1;

                return 0;
            });

            Repository.UpdateItemPrice(Arg.Any<IStockItem>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<IStockItem>(0);

                if (item.Name == StockItems[0].Name && item.Brand == StockItems[0].Brand)
                    return 1;

                return 0;
            });

            Repository.DeleteItem(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(0);
                var brand = callInfo.ArgAt<string>(1);

                if (name == StockItems[0].Name && brand == StockItems[0].Brand)
                    return 1;

                return 0;
            });

            Service = new StockService(Repository);
        }

        [Test]
        public async Task MustRegisterNewItem()
        {
            await Service.RegisterNewItem(ItemToRegister);

            await Repository.Received(1).GetItem(ItemToRegister.Name, ItemToRegister.Brand);
            await Repository.ReceivedWithAnyArgs(1).RegisterNewItem(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustNotRegisterNewItemIfAlreadyExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RegisterNewItem(StockItemsDtos[0]));

            await Repository.Received(1).GetItem(StockItemsDtos[0].Name, StockItemsDtos[0].Brand);
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToRegisterNewItem()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RegisterNewItem(ItemToFailRegister));

            await Repository.Received(1).GetItem(ItemToFailRegister.Name, ItemToFailRegister.Brand);
            await Repository.ReceivedWithAnyArgs(1).RegisterNewItem(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustAddItemAmount()
        {
            await Service.AddItemAmount(ItemToAddAmount);

            await Repository.Received(1).GetItem(StockItems[0].Name, StockItems[0].Brand);
            await Repository.Received(1).UpdateItemAmount(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustNotAddItemAmountIfItemDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddItemAmount(ItemToFailIntOperations));

            await Repository.Received(1).GetItem(ItemToRegister.Name, ItemToRegister.Brand);
            await Repository.Received(0).UpdateItemAmount(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustRemoveItemAmount()
        {
            await Service.RemoveItemAmount(ItemToAddAmount);

            await Repository.Received(1).GetItem(StockItems[0].Name, StockItems[0].Brand);
            await Repository.Received(1).UpdateItemAmount(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustNotRemoveItemAmountIfItemDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RemoveItemAmount(ItemToFailIntOperations));

            await Repository.Received(1).GetItem(ItemToRegister.Name, ItemToRegister.Brand);
            await Repository.Received(0).UpdateItemAmount(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustReserveItemAmount()
        {
            await Service.ReserveItemAmount(ItemToAddAmount);

            await Repository.Received(1).GetItem(StockItems[0].Name, StockItems[0].Brand);
            await Repository.Received(1).UpdateItemAmount(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustNotReserveItemAmountIfItemDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ReserveItemAmount(ItemToFailIntOperations));

            await Repository.Received(1).GetItem(ItemToRegister.Name, ItemToRegister.Brand);
            await Repository.Received(0).UpdateItemAmount(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustRestoreItemAmount()
        {
            await Service.RestoreItemAmount(ItemToAddAmount);

            await Repository.Received(1).GetItem(StockItems[0].Name, StockItems[0].Brand);
            await Repository.Received(1).UpdateItemAmount(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustNotRestoreItemAmountIfItemDoentExist()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RestoreItemAmount(ItemToFailIntOperations));

            await Repository.Received(1).GetItem(ItemToRegister.Name, ItemToRegister.Brand);
            await Repository.Received(0).UpdateItemAmount(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailUpdateItemAmount()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.AddItemAmount(ItemToFailAddAmount));

            await Repository.Received(1).GetItem(StockItems[1].Name, StockItems[1].Brand);
            await Repository.Received(1).UpdateItemAmount(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustGetItems()
        {
            var itens = (await Service.GetItens()).ToList();

            await Repository.Received(1).GetItens();

            Assert.That(itens, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(itens[0].Equals(StockItemsDtos[0]), Is.True);
                Assert.That(itens[1].Equals(StockItemsDtos[1]), Is.True);
            });
        }

        [Test]
        public async Task MustGetItem()
        {
            var item = await Service.GetItem(StockItems[0].Name, StockItems[0].Brand);

            await Repository.Received(1).GetItem(StockItems[0].Name, StockItems[0].Brand);

            Assert.That(item, Is.Not.Null);
            Assert.That(item.Equals(StockItemsDtos[0]), Is.True);
        }

        [Test]
        public async Task MustNotGetItemIfNotExists()
        {
            var item = await Service.GetItem(ItemToRegister.Name, ItemToRegister.Brand);

            await Repository.Received(1).GetItem(ItemToRegister.Name, ItemToRegister.Brand);

            Assert.That(item, Is.Null);
        }

        [Test]
        public async Task MustUpdateItemPrice()
        {
            await Service.UpdateItemPrice(ItemToUpdatePrice);

            await Repository.Received(1).GetItem(StockItems[0].Name, StockItems[0].Brand);
            await Repository.Received(1).UpdateItemPrice(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustNotUpdateItemPriceIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.UpdateItemPrice(ItemToFailDoubleOperations));

            await Repository.Received(1).GetItem(ItemToRegister.Name, ItemToRegister.Brand);
            await Repository.Received(0).UpdateItemPrice(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailtToUpdateItemPrice()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.UpdateItemPrice(ItemToFailUpdatePrice));

            await Repository.Received(1).GetItem(StockItems[1].Name, StockItems[1].Brand);
            await Repository.Received(1).UpdateItemPrice(Arg.Any<IStockItem>());
        }

        [Test]
        public async Task MustDeleteItem()
        {
            await Service.DeleteItem(StockItems[0].Name, StockItems[0].Brand);

            await Repository.Received(1).GetItem(StockItems[0].Name, StockItems[0].Brand);
            await Repository.Received(1).DeleteItem(StockItems[0].Name, StockItems[0].Brand);
        }

        [Test]
        public async Task MustNotDeleteItemIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeleteItem(ItemToRegister.Name, ItemToRegister.Brand));

            await Repository.Received(1).GetItem(ItemToRegister.Name, ItemToRegister.Brand);
            await Repository.Received(0).DeleteItem(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailToDeleteItem()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeleteItem(StockItems[1].Name, StockItems[1].Brand));

            await Repository.Received(1).GetItem(StockItems[1].Name, StockItems[1].Brand);
            await Repository.Received(1).DeleteItem(StockItems[1].Name, StockItems[1].Brand);
        }
    }
}
