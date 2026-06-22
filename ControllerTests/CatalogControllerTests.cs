using NSubstitute;
using Service.Interface;
using Service.Interface.Dto.Service;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class CatalogControllerTests : BaseControllerTests
    {
        private ICatalogService CatalogService { get; set; }

        private static readonly CreateServiceDto ServiceToRegister = new("Troca de Óleo", 2, 100, 1);
        private static Guid ExistingServiceId = Guid.NewGuid();
        private static readonly ServiceDto ExistingService = new(ExistingServiceId, "Revisão", 6, 150, 1);
        private static readonly CreateServiceDto ServiceToUpdate = new("Revisão Veicular", 4, 220, 1);

        protected override void MockService()
        {
            CatalogService = TestWebAppFactory.MechanicalServiceMock;

            CatalogService.RegisterService(Arg.Any<CreateServiceDto>()).Returns(callInfo =>
            {
                var service = callInfo.ArgAt<CreateServiceDto>(0);

                if (service.Equals(ServiceToRegister))
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            CatalogService.GetServices(id: Arg.Any<Guid?>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid?>(0);

                if (id != null)
                    return[ExistingService];

                return [ExistingService];
            });

            CatalogService.UpdateService(Arg.Any<Guid>(), Arg.Any<CreateServiceDto>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingServiceId)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            CatalogService.DeleteService(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingServiceId)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });
        }

        [Test]
        public async Task MustRegisterService()
        {
            var response = await TestClient.PostAsJsonAsync("catalog", ServiceToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await CatalogService.Received(1).RegisterService(ServiceToRegister);
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfFailToRegisterService()
        {
            var service = new CreateServiceDto("Teste", 1, 10, 1);
            var response = await TestClient.PostAsJsonAsync("catalog", service);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CatalogService.Received(1).RegisterService(service);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRegisterServiceWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("catalog", new { Description = "T", Hours = -1 });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CatalogService.ReceivedWithAnyArgs(0).RegisterService(Arg.Any<CreateServiceDto>());
        }

        [Test]
        public async Task MustGetServices()
        {
            var response = await TestClient.GetAsync("catalog");
            var content = await response.Content.ReadFromJsonAsync<List<ServiceDto>>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content, Has.Count.EqualTo(1));
            Assert.That(content[0], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(content[0].Id, Is.EqualTo(ExistingService.Id));
                Assert.That(content[0].Description, Is.EqualTo(ExistingService.Description));
                Assert.That(content[0].Hours, Is.EqualTo(ExistingService.Hours));
                Assert.That(content[0].PricePerHour, Is.EqualTo(ExistingService.PricePerHour));
                Assert.That(content[0].Amount, Is.EqualTo(ExistingService.Amount));
            });

            await CatalogService.Received(1).GetServices();
        }

        [Test]
        public async Task MustGetService()
        {
            var response = await TestClient.GetAsync($"catalog?id={ExistingService.Id}");
            var contents = await response.Content.ReadFromJsonAsync<List<ServiceDto>>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(contents, Has.Count.EqualTo(1));

            var content = contents[0];
            Assert.That(content, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(content.Id, Is.EqualTo(ExistingService.Id));
                Assert.That(content.Description, Is.EqualTo(ExistingService.Description));
                Assert.That(content.Hours, Is.EqualTo(ExistingService.Hours));
                Assert.That(content.PricePerHour, Is.EqualTo(ExistingService.PricePerHour));
                Assert.That(content.Amount, Is.EqualTo(ExistingService.Amount));
            });
            
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

            await CatalogService.Received(1).UpdateService(ExistingServiceId, ServiceToUpdate);
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryUpdateServiceThatNotExists()
        {
            var service = new CreateServiceDto(ExistingService.Description, ExistingService.Hours, ExistingService.PricePerHour, 1);

            var response = await TestClient.PatchAsJsonAsync($"catalog/{Guid.NewGuid()}", service);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CatalogService.Received(1).UpdateService(Arg.Any<Guid>(), service);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateServiceWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"catalog/{Guid.NewGuid()}", new {Description = "T"});

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CatalogService.ReceivedWithAnyArgs(0).UpdateService(Arg.Any<Guid>(), Arg.Any<ServiceDto>());
        }

        [Test]
        public async Task MustDeleteService()
        {
            var response = await TestClient.DeleteAsync($"catalog/{ExistingServiceId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await CatalogService.Received(1).DeleteService(ExistingServiceId);
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryDeleteServiceThatNotExists()
        {
            var response = await TestClient.DeleteAsync($"catalog/{Guid.NewGuid()}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await CatalogService.ReceivedWithAnyArgs(1).DeleteService(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteServiceWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync($"catalog/0000");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await CatalogService.ReceivedWithAnyArgs(0).DeleteService(Arg.Any<Guid>());
        }
    }
}

