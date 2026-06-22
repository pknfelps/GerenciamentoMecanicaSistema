using Dapper;
using Domain.Interface.Vehicle;
using Repository;
using Repository.Interface;
using NSubstitute;

namespace RepositoryTests
{
    public class VehicleRepositoryTests : BaseRepositoryTests
    {
        private IVehicleRepository VehicleRepository { get; set; }

        private static readonly string ExistingCustomerDocument = "123.456.789-12";

        private IVehicle VehicleToRegister
        {
            get
            {
                var vehicle = Substitute.For<IVehicle>();
                vehicle.Id.Returns(Guid.NewGuid());
                vehicle.CustomerDocument.Id.Returns(ExistingCustomerDocument);
                vehicle.Brand.Returns("Mitsubishi");
                vehicle.Model.Returns("Lancer Evolution X");
                vehicle.Year.Returns(2016);
                vehicle.LicensePlate.License.Returns("LNC1234");
                return vehicle;
            }
        }

        private static readonly Guid ExistingVehicleId = Guid.NewGuid();
        private static IVehicle ExistingVehicle
        {
            get
            {
                var vehicle = Substitute.For<IVehicle>();
                vehicle.Id.Returns(ExistingVehicleId);
                vehicle.CustomerDocument.Id.Returns(ExistingCustomerDocument);
                vehicle.Brand.Returns("Porsche");
                vehicle.Model.Returns("911");
                vehicle.Year.Returns(1990);
                vehicle.LicensePlate.License.Returns("PRX3911");
                return vehicle;
            }
        }

        private IVehicle VehicleToUpdate
        {
            get
            {
                var vehicle = Substitute.For<IVehicle>();
                vehicle.Id.Returns(ExistingVehicleId);
                vehicle.CustomerDocument.Id.Returns(ExistingCustomerDocument);
                vehicle.Brand.Returns("Porsche");
                vehicle.Model.Returns("911 Carrera");
                vehicle.Year.Returns(2002);
                vehicle.LicensePlate.License.Returns("PRX3911");
                return vehicle;
            }
        }

        protected override async Task InternalSetup()
        {
            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS customers (
                id UUID PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                document VARCHAR(100) NOT NULL UNIQUE,
                phone VARCHAR(100) NOT NULL,
                email VARCHAR(100) NOT NULL);
                """);

            await Connection.ExecuteAsync($"""
                INSERT INTO customers(id, name, document, phone, email)
                VALUES ('{Guid.NewGuid()}', 'Teste', '{ExistingCustomerDocument}', '(11) 91234-5678', 'teste@gmail.com');
                """);

            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS vehicles (
                id UUID PRIMARY KEY,
                customer_document VARCHAR(100) NOT NULL REFERENCES customers(document),
                brand VARCHAR(100) NOT NULL,
                model VARCHAR(100) NOT NULL,
                year INT NOT NULL,
                license_plate VARCHAR(7) NOT NULL UNIQUE);
                """);

            VehicleRepository = new VehicleRepository(Connection);

            await VehicleRepository.RegisterVehicle(ExistingVehicle);
        }

        [Test]
        public async Task MustRegisterVehicle()
        {
            var registry = await VehicleRepository.RegisterVehicle(VehicleToRegister);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustGetAllVehicles()
        {
            var vehicles = (await VehicleRepository.GetVehicles()).ToList();

            Assert.That(vehicles, Has.Count.EqualTo(1));

            Assert.Multiple(() =>
            {
                Assert.That(vehicles[0].Brand, Is.EqualTo(ExistingVehicle.Brand));
                Assert.That(vehicles[0].Model, Is.EqualTo(ExistingVehicle.Model));
                Assert.That(vehicles[0].Year, Is.EqualTo(ExistingVehicle.Year));
                Assert.That(vehicles[0].LicensePlate.License, Is.EqualTo(ExistingVehicle.LicensePlate.License));
            });
        }

        [Test]
        public async Task MustGetVehicleById()
        {
            var vehicle = await VehicleRepository.GetVehicle(id: ExistingVehicle.Id);

            Assert.That(vehicle, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(vehicle.Brand, Is.EqualTo(ExistingVehicle.Brand));
                Assert.That(vehicle.Model, Is.EqualTo(ExistingVehicle.Model));
                Assert.That(vehicle.Year, Is.EqualTo(ExistingVehicle.Year));
                Assert.That(vehicle.LicensePlate.License, Is.EqualTo(ExistingVehicle.LicensePlate.License));
            });
        }

        [Test]
        public async Task MustNotGetVehicleByIdIfNotExists()
        {
            var vehicle = await VehicleRepository.GetVehicle(id: Guid.NewGuid());

            Assert.That(vehicle, Is.Null);
        }

        [Test]
        public async Task MustGetVehicleByLicensePlate()
        {
            var vehicle = await VehicleRepository.GetVehicle(license_plate: ExistingVehicle.LicensePlate.License);

            Assert.That(vehicle, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(vehicle.Brand, Is.EqualTo(ExistingVehicle.Brand));
                Assert.That(vehicle.Model, Is.EqualTo(ExistingVehicle.Model));
                Assert.That(vehicle.Year, Is.EqualTo(ExistingVehicle.Year));
                Assert.That(vehicle.LicensePlate.License, Is.EqualTo(ExistingVehicle.LicensePlate.License));
            });
        }

        [Test]
        public async Task MustNotGetVehicleByLicensePlateIfNotExists()
        {
            var vehicle = await VehicleRepository.GetVehicle(license_plate: "AAA1234");

            Assert.That(vehicle, Is.Null);
        }

        [Test]
        public async Task MustUpdateVehicle()
        {
            var registry = await VehicleRepository.UpdateVehicle(VehicleToUpdate);

            Assert.That(registry, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustDeleteVehicle()
        {
            var registry = await VehicleRepository.DeleteVehicle(ExistingVehicle.Id);

            Assert.That(registry, Is.Not.EqualTo(0));
        }
    }
}
