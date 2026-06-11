using NSubstitute;
using Service.Interface;
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
        private static readonly PartUpdateDto<int> ItemToFailIntOperations = new(Guid.NewGuid(), 5);
        private static readonly PartUpdateDto<double> ItemToFailDoubleOperations = new(Guid.NewGuid(), 10.00);
        private static readonly PartUpdateDto<int> InvalidIntItemUpdate = new(Guid.NewGuid(), 0);
        private static readonly PartUpdateDto<double> InvalidDoubleItemUpdate = new(Guid.NewGuid(), 0);

        private static readonly List<PartDto> StockItems =
        [
            new (Guid.NewGuid(), "Vela de ignição", "Bosch", 6.00, 20, 5),
            new (Guid.NewGuid(), "Flúido para radiador", "Gitanes", 30.00, 5, 0)
        ];

        private static readonly PartUpdateDto<int> IntItemUpdate = new(StockItems[0].Id, 5);

        private static readonly PartUpdateDto<double> DoubleItemToUpdate = new(StockItems[0].Id, 8.45);

        protected override void MockService()
        {
            StockService = TestWebAppFactory.StockServiceMock;

            StockService.RegisterNewPart(Arg.Any<PartDto>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<PartDto>(0);

                if (item.Equals(ItemToRegister))
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.GetParts().Returns(StockItems);

            StockService.GetPart(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(0);
                var brand = callInfo.ArgAt<string>(1);

                return StockItems.FirstOrDefault(x => x.Name == name && x.Brand == brand);
            });

            StockService.AddPartAmount(Arg.Any<PartUpdateDto<int>>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<PartUpdateDto<int>>(0);

                if (StockItems.FirstOrDefault(x => x.Id == item.Id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.RemovePartAmount(Arg.Any<PartUpdateDto<int>>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<PartUpdateDto<int>>(0);

                if (StockItems.FirstOrDefault(x => x.Id == item.Id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.ReservePartAmount(Arg.Any<PartUpdateDto<int>>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<PartUpdateDto<int>>(0);

                if (StockItems.FirstOrDefault(x => x.Id == item.Id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.RestorePartAmount(Arg.Any<PartUpdateDto<int>>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<PartUpdateDto<int>>(0);

                if (StockItems.FirstOrDefault(x => x.Id == item.Id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.UpdatePartPrice(Arg.Any<PartUpdateDto<double>>()).Returns(callInfo =>
            {
                var item = callInfo.ArgAt<PartUpdateDto<double>>(0);

                if (StockItems.FirstOrDefault(x => x.Id == item.Id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.DeletePart(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(0);
                var brand = callInfo.ArgAt<string>(1);

                if (StockItems.FirstOrDefault(x => x.Name == name && x.Brand == brand) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });
        }

        [Test]
        public async Task MustRegisterItem()
        {
            var response = await TestClient.PostAsJsonAsync("/Stock/RegisterItem", ItemToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await StockService.Received(1).RegisterNewPart(ItemToRegister);
        }

        [Test]
        public async Task MustReturnBadRequestWhenTryRegisterItemWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("/Stock/RegisterItem", InvalidItemToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).RegisterNewPart(Arg.Any<PartDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfFailRegisterItem()
        {
            var response = await TestClient.PostAsJsonAsync("/Stock/RegisterItem", ItemToFailRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).RegisterNewPart(ItemToFailRegister);
        }

        [Test]
        public async Task MustGetItens()
        {
            var response = await TestClient.GetAsync("/Stock/GetItens");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<PartDto>>();
            var intens = result.ToList();

            await StockService.Received(1).GetParts();

            Assert.Multiple(() =>
            {
                Assert.That(intens[0].Equals(StockItems[0]), Is.True);
                Assert.That(intens[1].Equals(StockItems[1]), Is.True);
            });
        }

        [Test]
        public async Task MustGetItem()
        {
            var response = await TestClient.GetAsync($"/Stock/GetItem/{StockItems[0].Name}/{StockItems[0].Brand}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var result = await response.Content.ReadFromJsonAsync<PartDto>();

            await StockService.Received(1).GetPart(StockItems[0].Name, StockItems[0].Brand);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Equals(StockItems[0]), Is.True);
        }

        [Test]
        public async Task MustReturnNotFoundIfGetItemWithNotExistingItem()
        {
            var response = await TestClient.GetAsync($"/Stock/GetItem/{ItemToRegister.Name}/{ItemToRegister.Brand}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await StockService.Received(1).GetPart(ItemToRegister.Name, ItemToRegister.Brand);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetItemWithInvalidModel()
        {
            var response = await TestClient.GetAsync($"/Stock/GetItem/Teste123/Teste2");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).GetPart(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task MustAddItemAmount()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/AddItemAmount", IntItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await StockService.Received(1).AddPartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryAddItemAmountWithNotExistingItem()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/AddItemAmount", ItemToFailIntOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).AddPartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryAddItemAmountWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/AddItemAmount", InvalidIntItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).AddPartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustRemoveItemAmount()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/RemoveItemAmount", IntItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await StockService.Received(1).RemovePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryRemoveItemAmountWithNotExistingItem()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/RemoveItemAmount", ItemToFailIntOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).RemovePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRemoveItemAmountWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/RemoveItemAmount", InvalidIntItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).RemovePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustReserveItemAmount()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/ReserveItemAmount", IntItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await StockService.Received(1).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryReserveItemAmountWithNotExistingItem()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/ReserveItemAmount", ItemToFailIntOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryReserveItemAmountWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/ReserveItemAmount", InvalidIntItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).ReservePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustRestoreItemAmount()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/RestoreItemAmount", IntItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await StockService.Received(1).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryRestoreItemAmountWithNotExistingItem()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/RestoreItemAmount", ItemToFailIntOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRestoreItemAmountWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/RestoreItemAmount", InvalidIntItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).RestorePartAmount(Arg.Any<PartUpdateDto<int>>());
        }

        [Test]
        public async Task MustUpdateItemPrice()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/UpdateItemPrice", DoubleItemToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await StockService.Received(1).UpdatePartPrice(Arg.Any<PartUpdateDto<double>>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryUpdatePriceWithNoExistingItem()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/UpdateItemPrice", ItemToFailDoubleOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).UpdatePartPrice(Arg.Any<PartUpdateDto<double>>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateItemPriceWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"/Stock/UpdateItemPrice", InvalidDoubleItemUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).UpdatePartPrice(Arg.Any<PartUpdateDto<double>>());
        }

        [Test]
        public async Task MustDeleteItem()
        {
            var response = await TestClient.DeleteAsync($"/Stock/DeleteItem/{StockItems[0].Name}/{StockItems[0].Brand}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await StockService.Received(1).DeletePart(StockItems[0].Name, StockItems[0].Brand);
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryDeleteItemWithNoExistingItem()
        {
            var response = await TestClient.DeleteAsync($"/Stock/DeleteItem/{ItemToRegister.Name}/{ItemToRegister.Brand}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).DeletePart(ItemToRegister.Name, ItemToRegister.Brand);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteItemPriceWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync($"/Stock/DeleteItem/a/a");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).DeletePart(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
