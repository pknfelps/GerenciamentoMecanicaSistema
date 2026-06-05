using Dapper;
using Domain.Interface.Vehicle;
using Domain.Vehicle;
using Repository;
using Repository.Interface;

namespace RepositoryTests
{
    public class VehicleRepositoryTests : BaseRepositoryTests
    {
        private IVehicleRepository VehicleRepository { get; set; }

        private static Guid ExistingVehicleId = Guid.NewGuid();

        private List<IVehicle> ExistingVehicles =
        [
            new Vehicle(ExistingVehicleId, "Porsche", "911", 1990, "PRX3911"),
            new Vehicle("Dodge", "Challenger", 1980, "DOG3E80")
        ];

        private IVehicle VehicleToRegister = new Vehicle("Mitsubishi", "Lancer Evolution X", 2016, "LNC1234");
        private IVehicle VehicleToUpdate = new Vehicle(ExistingVehicleId, "Porsche", "911 Carrera", 2002, "PRC4911");

        protected override async Task InternalSetup()
        {
            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS vehicles (
                id UUID PRIMARY KEY,
                brand VARCHAR(100) NOT NULL,
                model VARCHAR(100) NOT NULL,
                year INT NOT NULL,
                license_plate VARCHAR(100) NOT NULL);
                """);

            VehicleRepository = new VehicleRepository(Connection);

            foreach (var vehicle in ExistingVehicles)
                await VehicleRepository.RegisterVehicle(vehicle);
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

            Assert.That(vehicles, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(vehicles[0].Brand, Is.EqualTo(ExistingVehicles[0].Brand));
                Assert.That(vehicles[0].Model, Is.EqualTo(ExistingVehicles[0].Model));
                Assert.That(vehicles[0].Year, Is.EqualTo(ExistingVehicles[0].Year));
                Assert.That(vehicles[0].LicensePlate.License, Is.EqualTo(ExistingVehicles[0].LicensePlate.License));
            });

            Assert.Multiple(() =>
            {
                Assert.That(vehicles[1].Brand, Is.EqualTo(ExistingVehicles[1].Brand));
                Assert.That(vehicles[1].Model, Is.EqualTo(ExistingVehicles[1].Model));
                Assert.That(vehicles[1].Year, Is.EqualTo(ExistingVehicles[1].Year));
                Assert.That(vehicles[1].LicensePlate.License, Is.EqualTo(ExistingVehicles[1].LicensePlate.License));
            });
        }

        [Test]
        public async Task MustGetVehicle()
        {
            var vehicle = await VehicleRepository.GetVehicle(ExistingVehicles[0].LicensePlate.License);

            Assert.That(vehicle, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(vehicle.Brand, Is.EqualTo(ExistingVehicles[0].Brand));
                Assert.That(vehicle.Model, Is.EqualTo(ExistingVehicles[0].Model));
                Assert.That(vehicle.Year, Is.EqualTo(ExistingVehicles[0].Year));
                Assert.That(vehicle.LicensePlate.License, Is.EqualTo(ExistingVehicles[0].LicensePlate.License));
            });
        }

        [Test]
        public async Task MustNotGetVehicleIfNotExists()
        {
            var vehicle = await VehicleRepository.GetVehicle("AAA1234");

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
            var registry = await VehicleRepository.DeleteVehicle(ExistingVehicles[0].LicensePlate.License);

            Assert.That(registry, Is.Not.EqualTo(0));
        }
    }
}
