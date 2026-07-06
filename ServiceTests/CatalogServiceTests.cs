using Domain.Interface.Service;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Commands.Catalog;
using Service.Interface.Results.Catalog;

namespace ServiceTests
{
    public class CatalogServiceTests
    {
        private ICatalogService Service { get; set; }
        private ICatalogRepository Repository { get; set; }

        private static CreateServiceCommand ServiceToCreate { get; } = new("Troca de Óleo", 2, 50, 1);
        private static CreateServiceCommand ServiceToFailCreation { get; } = new("Teste", 1, 10, 1);
        private static CreateServiceCommand ExistingServiceToCreate { get; } = new("Revisão", 6, 100, 1);

        private static readonly Guid ExistingServiceId = Guid.NewGuid();
        private static IMechanicalService ExistingService
        {
            get
            {
                var service = Substitute.For<IMechanicalService>();
                service.Id.Returns(ExistingServiceId);
                service.Description.Returns("Revisão");
                service.Hours.Returns(6);
                service.PricePerHour.Returns(100);
                service.Price.Returns(600);
                service.Amount.Returns(1);

                service.When(x => x.UpdateDescriptrion(Arg.Any<string>())).Do(callInfo =>
                {
                    var newDescription = callInfo.ArgAt<string>(0);

                    service.Description.Returns(newDescription);
                });

                return service;
            }
        }

        private static readonly Guid ExistingService2Id = Guid.NewGuid();
        private static IMechanicalService ExistingService2
        {
            get
            {
                var service = Substitute.For<IMechanicalService>();
                service.Id.Returns(ExistingService2Id);
                service.Description.Returns("Troca de Pneu");
                service.Hours.Returns(2);
                service.PricePerHour.Returns(50);
                service.Price.Returns(100);
                service.Amount.Returns(1);
                return service;
            }
        }

        private static CreateServiceCommand ServiceToUpdate { get; } = new("Revisão Veicular", 4, 150, 1);

        [SetUp]
        public async Task SetUp()
        {
            Repository = Substitute.For<ICatalogRepository>();

            Repository.RegisterService(Arg.Any<IMechanicalService>()).Returns(callInfo =>
            {
                var service = callInfo.ArgAt<IMechanicalService>(0);

                if (service.Description == ServiceToCreate.Description)
                    return 1;

                return 0;
            });

            List<IMechanicalService> services = new List<IMechanicalService>() { ExistingService, ExistingService2 };
            Repository.GetServices().Returns(services);

            Repository.GetService(Arg.Any<Guid>(), Arg.Any<string>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                var description = callInfo.ArgAt<string>(1);

                return services.FirstOrDefault(x => x.Id == id && x.Description == description);
            });

            Repository.GetService(description: Arg.Any<string>()).Returns(callInfo =>
            {
                var description = callInfo.ArgAt<string>(1);

                return services.FirstOrDefault(x => x.Description == description);
            });

            Repository.GetService(id: Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                return services.FirstOrDefault(x => x.Id == id);
            });

            Repository.UpdateService(Arg.Any<IMechanicalService>()).Returns(callInfo =>
            {
                var service = callInfo.ArgAt<IMechanicalService>(0);

                if (service.Description == ServiceToUpdate.Description)
                    return 1;

                return 0;
            });

            Repository.DeleteService(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id == ExistingServiceId)
                    return 1;

                return 0;
            });

            Service = new CatalogService(Repository);
        }

        [Test]
        public async Task MustRegisterService()
        {
            await Service.RegisterService(ServiceToCreate);

            await Repository.Received(1).GetService(description: ServiceToCreate.Description);
            await Repository.Received(1).RegisterService(Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustNotRegisterServiceIfAlreadyExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RegisterService(ExistingServiceToCreate));

            await Repository.Received(1).GetService(description: ExistingServiceToCreate.Description);
            await Repository.Received(0).RegisterService(Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustFailRegisterService()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.RegisterService(ServiceToFailCreation));

            await Repository.Received(1).GetService(description: ServiceToFailCreation.Description);
            await Repository.Received(1).RegisterService(Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustGetServices()
        {
            var services = await Service.GetServices();
            var servicesList = services.ToList();

            await Repository.Received(1).GetServices();

            Assert.That(servicesList, Has.Count.EqualTo(2));
            Assert.That(servicesList[0], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(servicesList[0].Id, Is.EqualTo(ExistingService.Id));
                Assert.That(servicesList[0].Description, Is.EqualTo(ExistingService.Description));
                Assert.That(servicesList[0].Hours, Is.EqualTo(ExistingService.Hours));
                Assert.That(servicesList[0].PricePerHour, Is.EqualTo(ExistingService.PricePerHour));
                Assert.That(servicesList[0].Amount, Is.EqualTo(ExistingService.Amount));
            });

            Assert.That(servicesList[1], Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(servicesList[1].Id, Is.EqualTo(ExistingService2.Id));
                Assert.That(servicesList[1].Description, Is.EqualTo(ExistingService2.Description));
                Assert.That(servicesList[1].Hours, Is.EqualTo(ExistingService2.Hours));
                Assert.That(servicesList[1].PricePerHour, Is.EqualTo(ExistingService2.PricePerHour));
                Assert.That(servicesList[1].Amount, Is.EqualTo(ExistingService2.Amount));
            });
        }

        [Test]
        public async Task MustGerServiceById()
        {
            var service = await Service.GetService(ExistingServiceId);

            Assert.That(service, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(service.Id, Is.EqualTo(ExistingService.Id));
                Assert.That(service.Description, Is.EqualTo(ExistingService.Description));
                Assert.That(service.Hours, Is.EqualTo(ExistingService.Hours));
                Assert.That(service.PricePerHour, Is.EqualTo(ExistingService.PricePerHour));
                Assert.That(service.Amount, Is.EqualTo(ExistingService.Amount));
            });
        }

        [Test]
        public async Task MustNotGerServiceByIdIfNotExists()
        {
            var service = await Service.GetService(Guid.NewGuid());

            Assert.That(service, Is.Null);
        }

        [Test]
        public async Task MustGetServiceByDescription()
        {
            var service = await Service.GetService(description: ExistingService.Description);

            Assert.That(service, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(service.Id, Is.EqualTo(ExistingService.Id));
                Assert.That(service.Description, Is.EqualTo(ExistingService.Description));
                Assert.That(service.Hours, Is.EqualTo(ExistingService.Hours));
                Assert.That(service.PricePerHour, Is.EqualTo(ExistingService.PricePerHour));
                Assert.That(service.Amount, Is.EqualTo(ExistingService.Amount));
            });
        }

        [Test]
        public async Task MustNotGetServiceByDescriptionIfNotExists()
        {
            var service = await Service.GetService(description: "a");

            Assert.That(service, Is.Null);
        }

        [Test]
        public async Task MustNotGetServiceIfNoParameterWasGiven()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.GetService());
        }

        [Test]
        public async Task MustUpdateService()
        {
            await Service.UpdateService(ExistingServiceId, ServiceToUpdate);

            await Repository.Received(1).GetService(ExistingServiceId);
            await Repository.Received(1).UpdateService(Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustNotUpdateServiceIfNotExists()
        {
            var service = new CreateServiceCommand("Revisão Automotiva", 4, 150, 1);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.UpdateService(Guid.NewGuid(), service));

            await Repository.Received(1).GetService(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateService(Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustFailToUpdateService()
        {
            var service = new CreateServiceCommand("Revisão Automotiva", 4, 150, 1);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.UpdateService(ExistingServiceId, service));

            await Repository.Received(1).GetService(ExistingServiceId);
            await Repository.Received(1).UpdateService(Arg.Any<IMechanicalService>());
        }

        [Test]
        public async Task MustDeleteService()
        {
            await Service.DeleteService(ExistingServiceId);

            await Repository.Received(1).GetService(ExistingServiceId);
            await Repository.Received(1).DeleteService(ExistingServiceId);
        }

        [Test]
        public async Task MustNotDeleteServiceIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeleteService(Guid.NewGuid()));

            await Repository.Received(1).GetService(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).DeleteService(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustFailToDeleteService()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.DeleteService(ExistingService2Id));

            await Repository.Received(1).GetService(ExistingService2Id);
            await Repository.Received(1).DeleteService(ExistingService2Id);
        }
    }
}
