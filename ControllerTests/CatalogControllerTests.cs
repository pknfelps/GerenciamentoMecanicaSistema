using GerenciamentoMecanicaSistema.Contracts.Requests.Catalog;
using GerenciamentoMecanicaSistema.Contracts.Responses.Catalog;
using NSubstitute;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Catalog;
using Service.Interface.Results.Catalog;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class CatalogControllerTests : BaseControllerTests
    {
        private ICatalogService CatalogService { get; set; }

        private static readonly CreateServiceRequest ServiceToRegister = new("Troca de Óleo", 2, 100, 1);
        private static readonly Guid ExistingServiceId = Guid.NewGuid();
        private static readonly ServiceResult ExistingService = new(ExistingServiceId, "Revisão", 6, 150, 1);
        private static readonly CreateServiceRequest ServiceToUpdate = new("Revisão Veicular", 4, 220, 1);

        protected override void MockService()
        {
            CatalogService = TestWebAppFactory.MechanicalServiceMock;

            CatalogService.RegisterService(Arg.Any<CreateServiceCommand>()).Returns(callInfo =>
            {
                var service = callInfo.ArgAt<CreateServiceCommand>(0);

                if (service.Equals(ServiceToRegister.ToCommand()))
                    return Task.CompletedTask;

                throw new ApplicationFailureException("Falha interna");
            });

            CatalogService.GetServices(id: Arg.Any<Guid?>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid?>(0);

                if (id != null)
                    return [ExistingService];

                return [ExistingService];
            });

            CatalogService.UpdateService(Arg.Any<Guid>(), Arg.Any<CreateServiceCommand>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingServiceId)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            CatalogService.DeleteService(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingServiceId)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });
        }

        [Test]
        public async Task MustRegisterService()
        {
            var response = await TestClient.PostAsJsonAsync("catalog", ServiceToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await CatalogService.Received(1).RegisterService(ServiceToRegister.ToCommand());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfFailToRegisterService()
        {
            var service = new CreateServiceRequest("Teste", 1, 10, 1);
            var response = await TestClient.PostAsJsonAsync("catalog", service);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CatalogService.Received(1).RegisterService(service.ToCommand());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRegisterServiceWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("catalog", new { Description = "T", Hours = -1 });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CatalogService.ReceivedWithAnyArgs(0).RegisterService(Arg.Any<CreateServiceCommand>());
        }

        [Test]
        public async Task MustGetServices()
        {
            var response = await TestClient.GetAsync("catalog");
            var content = await response.Content.ReadFromJsonAsync<List<ServiceResponse>>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content, Has.Count.EqualTo(1));
            Assert.That(content[0], Is.Not.Null);
            AssertService(content[0], ExistingService);

            await CatalogService.Received(1).GetServices();
        }

        [Test]
        public async Task MustGetService()
        {
            var response = await TestClient.GetAsync($"catalog?id={ExistingService.Id}");
            var contents = await response.Content.ReadFromJsonAsync<List<ServiceResponse>>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(contents, Has.Count.EqualTo(1));

            var content = contents[0];
            Assert.That(content, Is.Not.Null);
            AssertService(content, ExistingService);

            await CatalogService.Received(1).GetServices(ExistingService.Id);
        }

        [Test]
        public async Task MustReturnBadRequestIfGuidIsInvalidGetService()
        {
            var response = await TestClient.GetAsync($"catalog?id=0000");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CatalogService.ReceivedWithAnyArgs(0).GetService(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustUpdateService()
        {
            var response = await TestClient.PatchAsJsonAsync($"catalog/{ExistingServiceId}", ServiceToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await CatalogService.Received(1).UpdateService(ExistingServiceId, ServiceToUpdate.ToCommand());
        }

        [Test]
        public async Task MustReturnNotFoundIfTryUpdateServiceThatNotExists()
        {
            var service = new CreateServiceRequest(ExistingService.Description, ExistingService.Hours, ExistingService.PricePerHour, 1);

            var response = await TestClient.PatchAsJsonAsync($"catalog/{Guid.NewGuid()}", service);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await CatalogService.Received(1).UpdateService(Arg.Any<Guid>(), service.ToCommand());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateServiceWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"catalog/{Guid.NewGuid()}", new { Description = "T" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CatalogService.ReceivedWithAnyArgs(0).UpdateService(Arg.Any<Guid>(), Arg.Any<CreateServiceCommand>());
        }

        [Test]
        public async Task MustDeleteService()
        {
            var response = await TestClient.DeleteAsync($"catalog/{ExistingServiceId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await CatalogService.Received(1).DeleteService(ExistingServiceId);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryDeleteServiceThatNotExists()
        {
            var response = await TestClient.DeleteAsync($"catalog/{Guid.NewGuid()}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await CatalogService.ReceivedWithAnyArgs(1).DeleteService(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteServiceWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync($"catalog/0000");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CatalogService.ReceivedWithAnyArgs(0).DeleteService(Arg.Any<Guid>());
        }

        private static void AssertService(ServiceResponse response, ServiceResult result)
        {
            Assert.Multiple(() =>
            {
                Assert.That(response.Id, Is.EqualTo(result.Id));
                Assert.That(response.Description, Is.EqualTo(result.Description));
                Assert.That(response.Hours, Is.EqualTo(result.Hours));
                Assert.That(response.PricePerHour, Is.EqualTo(result.PricePerHour));
                Assert.That(response.Amount, Is.EqualTo(result.Amount));
            });
        }
    }
}




