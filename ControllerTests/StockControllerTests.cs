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

        private static readonly CreateMaterialDto MaterialToRegister = new("Óleo de motor", "Lubrax", 41.90, 5);
        private static readonly CreateMaterialDto InvalidMaterialToRegister = new("", "", 0.00, 0);
        private static readonly CreateMaterialDto MaterialToFailRegister = new("Teste", "Testando", 15, 1);
        private static readonly ValueUpdateDto<int> MaterialToFailIntOperations = new(5);
        private static readonly ValueUpdateDto<double> MaterialToFailDoubleOperations = new(10.00);
        private static readonly ValueUpdateDto<int> InvalidIntMaterialUpdate = new(0);
        private static readonly ValueUpdateDto<double> InvalidDoubleMaterialUpdate = new(0);

        private static readonly List<MaterialDto> StockMaterials =
        [
            new (Guid.NewGuid(), "Vela de ignição", "Bosch", 6.00, 20, 5),
            new (Guid.NewGuid(), "Flúido para radiador", "Gitanes", 30.00, 5, 0)
        ];

        private static readonly ValueUpdateDto<int> IntMaterialUpdate = new(5);

        private static readonly ValueUpdateDto<double> DoubleMaterialToUpdate = new(8.45);

        protected override void MockService()
        {
            StockService = TestWebAppFactory.StockServiceMock;

            StockService.RegisterNewMaterial(Arg.Any<CreateMaterialDto>()).Returns(callInfo =>
            {
                var material = callInfo.ArgAt<CreateMaterialDto>(0);

                if (material.Equals(MaterialToRegister))
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.GetMaterials(name: Arg.Any<string>(), brand: Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(1);
                var brand = callInfo.ArgAt<string>(2);

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(brand))
                    return StockMaterials.Where(x => x.Name == name && x.Brand == brand);

                return StockMaterials;
            });

            StockService.AddMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.RemoveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.UpdateMaterialPrice(Arg.Any<Guid>(), Arg.Any<double>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            StockService.DeleteMaterial(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });
        }

        [Test]
        public async Task MustRegisterMaterial()
        {
            var response = await TestClient.PostAsJsonAsync("stock", MaterialToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await StockService.Received(1).RegisterNewMaterial(MaterialToRegister);
        }

        [Test]
        public async Task MustReturnBadRequestWhenTryRegisterMaterialWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("stock", InvalidMaterialToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).RegisterNewMaterial(Arg.Any<MaterialDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfFailRegisterMaterial()
        {
            var response = await TestClient.PostAsJsonAsync("stock", MaterialToFailRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).RegisterNewMaterial(MaterialToFailRegister);
        }

        [Test]
        public async Task MustGetMaterials()
        {
            var response = await TestClient.GetAsync("stock");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<MaterialDto>>();
            var intems = result.ToList();

            await StockService.Received(1).GetMaterials();

            Assert.That(intems, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(intems[0].Equals(StockMaterials[0]), Is.True);
                Assert.That(intems[1].Equals(StockMaterials[1]), Is.True);
            });
        }

        [Test]
        public async Task MustGetMaterial()
        {
            var response = await TestClient.GetAsync($"stock?name={StockMaterials[0].Name}&brand={StockMaterials[0].Brand}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var materials = await response.Content.ReadFromJsonAsync<List<MaterialDto>>();

            await StockService.Received(1).GetMaterials(name: StockMaterials[0].Name, brand: StockMaterials[0].Brand);
            Assert.That(materials, Has.Count.EqualTo(1));

            var material = materials[0];
            Assert.That(material, Is.Not.Null);
            Assert.That(material.Equals(StockMaterials[0]), Is.True);
        }

        [Test]
        public async Task MustAddMaterialAmount()
        {
            var response = await TestClient.PostAsJsonAsync($"stock/amount/{StockMaterials[0].Id}", IntMaterialUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await StockService.Received(1).AddMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryAddMaterialAmountWithNotExistingMaterial()
        {
            var response = await TestClient.PostAsJsonAsync($"stock/amount/{Guid.NewGuid()}", MaterialToFailIntOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).AddMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryAddMaterialAmountWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/amount/0000", InvalidIntMaterialUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).AddMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustRemoveMaterialAmount()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/amount/{StockMaterials[0].Id}", IntMaterialUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await StockService.Received(1).RemoveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryRemoveMaterialAmountWithNotExistingMaterial()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/amount/{Guid.NewGuid()}", MaterialToFailIntOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).RemoveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRemoveMaterialAmountWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/amount/0000", InvalidIntMaterialUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).RemoveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustUpdateMaterialPrice()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/price/{StockMaterials[0].Id}", DoubleMaterialToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await StockService.Received(1).UpdateMaterialPrice(Arg.Any<Guid>(), Arg.Any<double>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryUpdatePriceWithNoExistingMaterial()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/price/{Guid.NewGuid()}", MaterialToFailDoubleOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).UpdateMaterialPrice(Arg.Any<Guid>(), Arg.Any<double>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateMaterialPriceWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/price/0000", InvalidDoubleMaterialUpdate.Value);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).UpdateMaterialPrice(Arg.Any<Guid>(), Arg.Any<double>());
        }

        [Test]
        public async Task MustDeleteMaterial()
        {
            var response = await TestClient.DeleteAsync($"stock/{StockMaterials[0].Id}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await StockService.Received(1).DeleteMaterial(StockMaterials[0].Id);
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryDeleteMaterialWithNoExistingMaterial()
        {
            var response = await TestClient.DeleteAsync($"stock/{Guid.NewGuid()}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).DeleteMaterial(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteMaterialPriceWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync($"stock/aa");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).DeleteMaterial(Arg.Any<Guid>());
        }
    }
}
