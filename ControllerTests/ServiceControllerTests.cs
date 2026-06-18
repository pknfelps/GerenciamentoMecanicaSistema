using NSubstitute;
using NUnit.Framework.Constraints;
using Service.Interface;
using Service.Interface.Dto.Service;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class ServiceControllerTests : BaseControllerTests
    {
        private IMechanicalServiceService MechanicalService { get; set; }

        private static readonly CreateServiceDto ServiceToRegister = new("Troca de Óleo", 2, 100);
        private static Guid ExistingServiceId = Guid.NewGuid();
        private static readonly ServiceDto ExistingService = new(ExistingServiceId, "Revisão", 6, 150, 1);
        private static readonly ServiceDto ServiceToUpdate = new(ExistingServiceId, "Revisão Veicular", 4, 220, 1);

        protected override void MockService()
        {
            MechanicalService = TestWebAppFactory.MechanicalServiceMock;

            MechanicalService.RegisterService(Arg.Any<CreateServiceDto>()).Returns(callInfo =>
            {
                var service = callInfo.ArgAt<CreateServiceDto>(0);

                if (service.Equals(ServiceToRegister))
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            MechanicalService.GetServices().Returns([ExistingService]);

            MechanicalService.GetService(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingService.Id)
                    return ExistingService;

                return null;
            });

            MechanicalService.UpdateService(Arg.Any<ServiceDto>()).Returns(callInfo =>
            {
                var service = callInfo.ArgAt<ServiceDto>(0);

                if (service.Id == ExistingService.Id)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            MechanicalService.DeleteService(Arg.Any<Guid>()).Returns(callInfo =>
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
            var response = await TestClient.PostAsJsonAsync("Service/RegisterService", ServiceToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await MechanicalService.Received(1).RegisterService(ServiceToRegister);
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfFailToRegisterService()
        {
            var service = new CreateServiceDto("Teste", 1, 10);
            var response = await TestClient.PostAsJsonAsync("Service/RegisterService", service);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await MechanicalService.Received(1).RegisterService(service);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRegisterServiceWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("Service/RegisterService", new { Description = "T", Hours = -1 });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await MechanicalService.ReceivedWithAnyArgs(0).RegisterService(Arg.Any<CreateServiceDto>());
        }

        [Test]
        public async Task MustGetServices()
        {
            var response = await TestClient.GetAsync("Service/GetServices");
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

            await MechanicalService.Received(1).GetServices();
        }

        [Test]
        public async Task MustGetService()
        {
            var response = await TestClient.GetAsync($"Service/GetService/{ExistingService.Id}");
            var content = await response.Content.ReadFromJsonAsync<ServiceDto>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(content.Id, Is.EqualTo(ExistingService.Id));
                Assert.That(content.Description, Is.EqualTo(ExistingService.Description));
                Assert.That(content.Hours, Is.EqualTo(ExistingService.Hours));
                Assert.That(content.PricePerHour, Is.EqualTo(ExistingService.PricePerHour));
                Assert.That(content.Amount, Is.EqualTo(ExistingService.Amount));
            });
            
            await MechanicalService.Received(1).GetService(ExistingService.Id);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryGetServiceWithUnexistingId()
        {
            var response = await TestClient.GetAsync($"Service/GetService/{Guid.NewGuid()}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await MechanicalService.ReceivedWithAnyArgs(1).GetService(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustReturnBadRequestIfGuidIsInvalidGetService()
        {
            var response = await TestClient.GetAsync($"Service/GetService/0000");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await MechanicalService.ReceivedWithAnyArgs(0).GetService(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustUpdateService()
        {
            var response = await TestClient.PatchAsJsonAsync($"Service/UpdateService", ServiceToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await MechanicalService.Received(1).UpdateService(ServiceToUpdate);
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryUpdateServiceThatNotExists()
        {
            var service = new ServiceDto(Guid.NewGuid(), ExistingService.Description, ExistingService.Hours, ExistingService.PricePerHour, 1);

            var response = await TestClient.PatchAsJsonAsync($"Service/UpdateService", service);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await MechanicalService.Received(1).UpdateService(service);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateServiceWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync($"Service/UpdateService", new {Description = "T"});

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await MechanicalService.ReceivedWithAnyArgs(0).UpdateService(Arg.Any<ServiceDto>());
        }

        [Test]
        public async Task MustDeleteService()
        {
            var response = await TestClient.DeleteAsync($"Service/DeleteService/{ExistingServiceId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await MechanicalService.Received(1).DeleteService(ExistingServiceId);
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfTryDeleteServiceThatNotExists()
        {
            var response = await TestClient.DeleteAsync($"Service/DeleteService/{Guid.NewGuid()}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await MechanicalService.ReceivedWithAnyArgs(1).DeleteService(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteServiceWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync($"Service/DeleteService/0000");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await MechanicalService.ReceivedWithAnyArgs(0).DeleteService(Arg.Any<Guid>());
        }
    }
}

