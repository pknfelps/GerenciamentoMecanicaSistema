using NSubstitute;
using Service.Interface;
using Service.Interface.Dto;
using Service.Interface.Dto.Stock;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class StockControllerTests : BaseControllerTests
    {
        private IStockService StockService { get; set; }

        private static readonly CreatePartDto ItemToRegister = new("Óleo de motor", "Lubrax", 41.90, 5);
        private static readonly CreatePartDto InvalidItemToRegister = new("", "", 0.00, 0);
        private static readonly CreatePartDto ItemToFailRegister = new("Teste", "Testando", 15, 1);
        private static readonly UpdateItemDto<int> ItemToFailIntOperations = new(Guid.NewGuid(), 5);
        private static readonly UpdateItemDto<double> ItemToFailDoubleOperations = new(Guid.NewGuid(), 10.00);
        private static readonly UpdateItemDto<int> InvalidIntItemUpdate = new(Guid.NewGuid(), 0);
        private static readonly UpdateItemDto<double> InvalidDoubleItemUpdate = new(Guid.NewGuid(), 0);

        private static readonly List<PartDto> StockItems =
        [
            new (Guid.NewGuid(), "Vela de ignição", "Bosch", 6.00, 20, 5),
            new (Guid.NewGuid(), "Flúido para radiador", "Gitanes", 30.00, 5, 0)
        ];

        private static readonly UpdateItemDto<int> IntItemUpdate = new(StockItems[0].Id, 5);

        private static readonly UpdateItemDto<double> DoubleItemToUpdate = new(StockItems[0].Id, 8.45);

        protected override void MockService()
        {
            StockService = TestWebAppFactory.StockServiceMock;

            StockService.RegisterNewPart(Arg.Any<CreatePartDto>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<CreatePartDto>(0);

                if (item.Equals(ItemToRegister))
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.GetParts(name: Arg.Any<string>(), brand: Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(1);
                var brand = callInfo.ArgAt<string>(2);

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(brand))
                    return StockItems.Where(x => x.Name == name && x.Brand == brand);

                return StockItems;
            });

            StockService.AddPartAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockItems.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.RemovePartAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockItems.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.ReservePartAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockItems.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.RestorePartAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockItems.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.UpdatePartPrice(Arg.Any<Guid>(), Arg.Any<double>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockItems.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.DeletePart(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockItems.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });
        }

        [Test]
        public async Task MustRegisterItem()
        {
            var response = await TestClient.PostAsJsonAsync("stock", ItemToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await StockService.Received(1).RegisterNewPart(ItemToRegister);
        }

        [Test]
        public async Task MustReturnBadRequestWhenTryRegisterItemWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("stock", InvalidItemToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).RegisterNewPart(Arg.Any<PartDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfFailRegisterItem()
        {
            var response = await TestClient.PostAsJsonAsync("stock", ItemToFailRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).RegisterNewPart(ItemToFailRegister);
        }

        [Test]
        public async Task MustGetItems()
        {
            var response = await TestClient.GetAsync("stock");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<PartDto>>();
            var intems = result.ToList();

            await StockService.Received(1).GetParts();

            Assert.That(intems, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(intems[0].Equals(StockItems[0]), Is.True);
                Assert.That(intems[1].Equals(StockItems[1]), Is.True);
            });
        }

        [Test]
        public async Task MustGetItem()
        {
            var response = await TestClient.GetAsync($"stock?name={StockItems[0].Name}&brand={StockItems[0].Brand}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var items = await response.Content.ReadFromJsonAsync<List<PartDto>>();

            await StockService.Received(1).GetParts(name: StockItems[0].Name, brand: StockItems[0].Brand);
            Assert.That(items, Has.Count.EqualTo(1));

            var item = items[0];
            Assert.That(item, Is.Not.Null);
            Assert.That(item.Equals(StockItems[0]), Is.True);
        }

        [Test]
        public async Task MustAddItemAmount()
        {
            var response = await TestClient.PostAsJsonAsync($"stock/amount/{StockItems[0].Id}", IntItemUpdate.Value);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await StockService.Received(1).AddPartAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryAddItemAmountWithNotExistingItem()
        {
            var response = await TestClient.PostAsJsonAsync($"stock/amount/{Guid.NewGuid()}", ItemToFailIntOperations.Value);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).AddPartAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryAddItemAmountWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/amount/0000", InvalidIntItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).AddPartAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustRemoveItemAmount()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/amount/{StockItems[0].Id}", IntItemUpdate.Value);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await StockService.Received(1).RemovePartAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryRemoveItemAmountWithNotExistingItem()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/amount/{Guid.NewGuid()}", ItemToFailIntOperations.Value);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).RemovePartAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRemoveItemAmountWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/amount/0000", InvalidIntItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).RemovePartAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustUpdateItemPrice()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/price/{StockItems[0].Id}", DoubleItemToUpdate.Value);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await StockService.Received(1).UpdatePartPrice(Arg.Any<Guid>(), Arg.Any<double>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryUpdatePriceWithNoExistingItem()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/price/{Guid.NewGuid()}", ItemToFailDoubleOperations.Value);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).UpdatePartPrice(Arg.Any<Guid>(), Arg.Any<double>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateItemPriceWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/price/0000", InvalidDoubleItemUpdate.Value);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).UpdatePartPrice(Arg.Any<Guid>(), Arg.Any<double>());
        }

        [Test]
        public async Task MustDeleteItem()
        {
            var response = await TestClient.DeleteAsync($"stock/{StockItems[0].Id}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await StockService.Received(1).DeletePart(StockItems[0].Id);
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryDeleteItemWithNoExistingItem()
        {
            var response = await TestClient.DeleteAsync($"stock/{Guid.NewGuid()}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).DeletePart(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteItemPriceWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync($"stock/aa");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).DeletePart(Arg.Any<Guid>());
        }
    }
}
