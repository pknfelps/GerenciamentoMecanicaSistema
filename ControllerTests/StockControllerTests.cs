using GerenciamentoMecanicaSistema.Contracts.Requests.Stock;
using GerenciamentoMecanicaSistema.Contracts.Responses.Stock;
using NSubstitute;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Stock;
using Service.Interface.Results.Stock;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class StockControllerTests : BaseControllerTests
    {
        private IStockService StockService { get; set; }

        private static readonly CreateMaterialRequest MaterialToRegister = new("Óleo de motor", "Lubrax", 41.90m, 5);
        private static readonly CreateMaterialRequest InvalidMaterialToRegister = new("", "", 0.00m, 0);
        private static readonly CreateMaterialRequest MaterialToFailRegister = new("Teste", "Testando", 15, 1);
        private static readonly ValueUpdateRequest<int> MaterialToFailIntOperations = new(5);
        private static readonly ValueUpdateRequest<decimal> MaterialToFailDecimalOperations = new(10.00m);
        private static readonly ValueUpdateRequest<int> InvalidIntMaterialUpdate = new(0);
        private static readonly ValueUpdateRequest<decimal> InvalidDecimalMaterialUpdate = new(0);

        private static readonly List<MaterialResult> StockMaterials =
        [
            new (Guid.NewGuid(), "Vela de ignição", "Bosch", 6.00m, 20, 5),
            new (Guid.NewGuid(), "Flúido para radiador", "Gitanes", 30.00m, 5, 0)
        ];

        private static readonly ValueUpdateRequest<int> IntMaterialUpdate = new(5);

        private static readonly ValueUpdateRequest<decimal> DecimalMaterialToUpdate = new(8.45m);

        protected override void MockService()
        {
            StockService = TestWebAppFactory.StockServiceMock;

            StockService.RegisterNewMaterial(Arg.Any<CreateMaterialCommand>()).Returns(callInfo =>
            {
                var material = callInfo.ArgAt<CreateMaterialCommand>(0);

                if (material.Equals(MaterialToRegister.ToCommand()))
                    return Task.CompletedTask;

                throw new ApplicationFailureException("Falha interna");
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

                throw new NotFoundException("Recurso não encontrado");
            });

            StockService.RemoveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            StockService.ReserveMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            StockService.RestoreMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            StockService.UpdateMaterialPrice(Arg.Any<Guid>(), Arg.Any<decimal>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            StockService.DeleteMaterial(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (StockMaterials.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });
        }

        [Test]
        public async Task MustRegisterMaterial()
        {
            var response = await TestClient.PostAsJsonAsync("stock", MaterialToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await StockService.Received(1).RegisterNewMaterial(MaterialToRegister.ToCommand());
        }

        [Test]
        public async Task MustReturnBadRequestWhenTryRegisterMaterialWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("stock", InvalidMaterialToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).RegisterNewMaterial(Arg.Any<CreateMaterialCommand>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfFailRegisterMaterial()
        {
            var response = await TestClient.PostAsJsonAsync("stock", MaterialToFailRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await StockService.Received(1).RegisterNewMaterial(MaterialToFailRegister.ToCommand());
        }

        [Test]
        public async Task MustGetMaterials()
        {
            var response = await TestClient.GetAsync("stock");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var result = await response.Content.ReadFromJsonAsync<IEnumerable<MaterialResponse>>();
            var intems = result.ToList();

            await StockService.Received(1).GetMaterials();

            Assert.That(intems, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(intems[0].Id, Is.EqualTo(StockMaterials[0].Id));
                Assert.That(intems[1].Id, Is.EqualTo(StockMaterials[1].Id));
            });
        }

        [Test]
        public async Task MustGetMaterial()
        {
            var response = await TestClient.GetAsync($"stock?name={StockMaterials[0].Name}&brand={StockMaterials[0].Brand}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var materials = await response.Content.ReadFromJsonAsync<List<MaterialResponse>>();

            await StockService.Received(1).GetMaterials(name: StockMaterials[0].Name, brand: StockMaterials[0].Brand);
            Assert.That(materials, Has.Count.EqualTo(1));

            var material = materials[0];
            Assert.That(material, Is.Not.Null);
            Assert.That(material.Id, Is.EqualTo(StockMaterials[0].Id));
        }

        [Test]
        public async Task MustAddMaterialAmount()
        {
            var response = await TestClient.PostAsJsonAsync($"stock/amount/{StockMaterials[0].Id}", IntMaterialUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await StockService.Received(1).AddMaterialAmount(Arg.Any<Guid>(), Arg.Any<int>());
        }

        [Test]
        public async Task MustReturnNotFoundIfTryAddMaterialAmountWithNotExistingMaterial()
        {
            var response = await TestClient.PostAsJsonAsync($"stock/amount/{Guid.NewGuid()}", MaterialToFailIntOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

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
        public async Task MustReturnNotFoundIfTryRemoveMaterialAmountWithNotExistingMaterial()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/amount/{Guid.NewGuid()}", MaterialToFailIntOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

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
            var response = await TestClient.PatchAsJsonAsync($"stock/price/{StockMaterials[0].Id}", DecimalMaterialToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await StockService.Received(1).UpdateMaterialPrice(Arg.Any<Guid>(), Arg.Any<decimal>());
        }

        [Test]
        public async Task MustReturnNotFoundIfTryUpdatePriceWithNoExistingMaterial()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/price/{Guid.NewGuid()}", MaterialToFailDecimalOperations);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await StockService.Received(1).UpdateMaterialPrice(Arg.Any<Guid>(), Arg.Any<decimal>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateMaterialPriceWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"stock/price/0000", InvalidDecimalMaterialUpdate.Value);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await StockService.Received(0).UpdateMaterialPrice(Arg.Any<Guid>(), Arg.Any<decimal>());
        }

        [Test]
        public async Task MustDeleteMaterial()
        {
            var response = await TestClient.DeleteAsync($"stock/{StockMaterials[0].Id}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await StockService.Received(1).DeleteMaterial(StockMaterials[0].Id);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryDeleteMaterialWithNoExistingMaterial()
        {
            var response = await TestClient.DeleteAsync($"stock/{Guid.NewGuid()}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

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