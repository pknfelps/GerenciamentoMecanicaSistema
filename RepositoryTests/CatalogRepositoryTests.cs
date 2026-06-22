using Dapper;
using Domain.Interface.Service;
using NSubstitute;
using Repository;
using Repository.Interface;

namespace RepositoryTests
{
    public class CatalogRepositoryTests : BaseRepositoryTests
    {
        private ICatalogRepository Repository { get; set; }

        private static IMechanicalService ServiceToRegister
        {
            get
            {
                var service = Substitute.For<IMechanicalService>();
                service.Id.Returns(Guid.NewGuid());
                service.Description.Returns("Troca de Pneu");
                service.Hours.Returns(2);
                service.PricePerHour.Returns(150);
                service.Amount.Returns(1);
                service.Price.Returns(300);
                return service;
            }
        }

        private static readonly Guid ExistingServiceId = Guid.NewGuid();
        private static IMechanicalService ExistingService
        {
            get
            {
                var service = Substitute.For<IMechanicalService>();
                service.Id.Returns(ExistingServiceId);
                service.Description.Returns("Troca de Óleo");
                service.Hours.Returns(2);
                service.PricePerHour.Returns(50);
                service.Amount.Returns(1);
                service.Price.Returns(100);
                return service;
            }
        }

        private static IMechanicalService ServiceToUpdate
        {
            get
            {
                var service = Substitute.For<IMechanicalService>();
                service.Id.Returns(ExistingServiceId);
                service.Description.Returns("Troca Óleo");
                service.Hours.Returns(2);
                service.PricePerHour.Returns(75);
                service.Amount.Returns(1);
                service.Price.Returns(150);
                return service;
            }
        }

        protected override async Task InternalSetup()
        {
            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS services (
                id UUID PRIMARY KEY,
                description VARCHAR(255) NOT NULL,
                hours FLOAT NOT NULL,
                price_per_hour DOUBLE PRECISION NOT NULL);
                """);

            Repository = new CatalogRepository(Connection);

            await Repository.RegisterService(ExistingService);
        }

        [Test]
        public async Task MustRegisterService()
        {
            var registry = await Repository.RegisterService(ServiceToRegister);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustGetServices()
        {
            var services = await Repository.GetServices();
            var servicesList = services.ToList();

            Assert.That(servicesList, Has.Count.EqualTo(1));

            Assert.Multiple(() =>
            {
                Assert.That(servicesList[0], Is.Not.Null);
                Assert.That(servicesList[0].Id, Is.EqualTo(ExistingService.Id));
                Assert.That(servicesList[0].Description, Is.EqualTo(ExistingService.Description));
                Assert.That(servicesList[0].Hours, Is.EqualTo(ExistingService.Hours));
                Assert.That(servicesList[0].PricePerHour, Is.EqualTo(ExistingService.PricePerHour));
                Assert.That(servicesList[0].Amount, Is.EqualTo(ExistingService.Amount));
                Assert.That(servicesList[0].Price, Is.EqualTo(ExistingService.Price));
            });
        }

        [Test]
        public async Task MustGetServiceById()
        {
            var service = await Repository.GetService(ExistingServiceId);

            Assert.Multiple(() =>
            {
                Assert.That(service, Is.Not.Null);
                Assert.That(service.Id, Is.EqualTo(ExistingService.Id));
                Assert.That(service.Description, Is.EqualTo(ExistingService.Description));
                Assert.That(service.Hours, Is.EqualTo(ExistingService.Hours));
                Assert.That(service.PricePerHour, Is.EqualTo(ExistingService.PricePerHour));
                Assert.That(service.Amount, Is.EqualTo(ExistingService.Amount));
                Assert.That(service.Price, Is.EqualTo(ExistingService.Price));
            });
        }

        [Test]
        public async Task MustNotGetServiceByIdIfNotExists()
        {
            var service = await Repository.GetService(Guid.NewGuid());

            Assert.That(service, Is.Null);
        }

        [Test]
        public async Task MustGetServiceByDescription()
        {
            var service = await Repository.GetService(description: ExistingService.Description);

            Assert.Multiple(() =>
            {
                Assert.That(service, Is.Not.Null);
                Assert.That(service.Id, Is.EqualTo(ExistingService.Id));
                Assert.That(service.Description, Is.EqualTo(ExistingService.Description));
                Assert.That(service.Hours, Is.EqualTo(ExistingService.Hours));
                Assert.That(service.PricePerHour, Is.EqualTo(ExistingService.PricePerHour));
                Assert.That(service.Amount, Is.EqualTo(ExistingService.Amount));
                Assert.That(service.Price, Is.EqualTo(ExistingService.Price));
            });
        }

        [Test]
        public async Task MustNotGetServiceByDescriptionIfNotExists()
        {
            var service = await Repository.GetService(description: "a");

            Assert.That(service, Is.Null);
        }

        [Test]
        public async Task MustUpdateService()
        {
            var registry = await Repository.UpdateService(ServiceToUpdate);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustDeleteService()
        {
            var registry = await Repository.DeleteService(ExistingServiceId);

            Assert.That(registry, Is.Not.EqualTo(0));
        }
    }
}
