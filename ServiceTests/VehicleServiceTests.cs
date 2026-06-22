using Domain.Interface.Vehicle;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto.Vehicle;

namespace ServiceTests
{
    public class VehicleServiceTests
    {
        private IVehicleService VehicleService { get; set; }
        private IVehicleRepository Repository { get; set; }

        private static CreateVehicleDto VehicleToRegister { get; } = new("12345678912", "Fiat", "Mobi", 2025, "FIT4M08");
        private static CreateVehicleDto VehicleToFailRegister { get; } = new("12345678912", "Test", "Test", 0000, "TST1234");

        private static readonly Guid ExistingVehicleId = Guid.NewGuid();
        private static IVehicle ExistingVehicle
        {
            get
            {
                var vehicle = Substitute.For<IVehicle>();
                vehicle.Id.Returns(ExistingVehicleId);
                vehicle.Brand.Returns("Honda");
                vehicle.Model.Returns("Civic");
                vehicle.Year.Returns(2024);
                vehicle.LicensePlate.License.Returns("CVC2024");
                return vehicle;
            }
        }

        private static readonly Guid ExistingVehicle2Id = Guid.NewGuid();
        private static IVehicle ExistingVehicle2
        {
            get
            {
                var vehicle = Substitute.For<IVehicle>();
                vehicle.Id.Returns(ExistingVehicle2Id);
                vehicle.Brand.Returns("Ford");
                vehicle.Model.Returns("Ka");
                vehicle.Year.Returns(2020);
                vehicle.LicensePlate.License.Returns("FKA0F20");
                return vehicle;
            }
        }

        private static VehicleDto ExistingVehicleDto { get; } = new(Guid.NewGuid(), "12345678912", "Honda", "Civic", 2024, "CVC2024");
        private static CreateVehicleDto ExistingVehicleToUpdate { get; } = new("12345678912", "Honda", "City", 2020, "CVC2024");
        private static CreateVehicleDto ExistingVehicleToFailUpdateOrDelete { get; } = new("12345678912", "Test", "Test", 2020, "FKA0F20");

        [SetUp]
        public void SetUp()
        {
            Repository = Substitute.For<IVehicleRepository>();

            Repository.RegisterVehicle(Arg.Any<IVehicle>()).Returns(callInfo =>
            {
                var vehicle = callInfo.ArgAt<IVehicle>(0);

                if (vehicle.LicensePlate.License.Equals(VehicleToRegister.LicensePlate))
                    return 1;

                return 0;
            });

            List<IVehicle> vehicles = new List<IVehicle>() { ExistingVehicle, ExistingVehicle2 };

            Repository.GetVehicles(license_plate: Arg.Any<string>()).Returns(callInfo =>
            {
                var license = callInfo.ArgAt<string>(1);

                if (license.Equals(ExistingVehicle.LicensePlate.License))
                    return [ExistingVehicle];

                return vehicles;
            });

            Repository.GetVehicle(license_plate: Arg.Any<string>()).Returns(callInfo =>
            {
                var license = callInfo.ArgAt<string>(1);

                return vehicles.FirstOrDefault(x => x.LicensePlate.License.Equals(license));
            });

            Repository.GetVehicle(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                return vehicles.FirstOrDefault(x => x.Id == id);
            });

            Repository.UpdateVehicle(Arg.Any<IVehicle>()).Returns(callInfo =>
            {
                var vehicle = callInfo.ArgAt<IVehicle>(0);

                if (vehicle.Brand == VehicleToFailRegister.Brand && vehicle.Model == VehicleToFailRegister.Model)
                    return 0;

                if (vehicles.FirstOrDefault(x => x.LicensePlate.License.Equals(vehicle.LicensePlate.License)) != default)
                    return 1;

                return 0;
            });

            Repository.DeleteVehicle(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (vehicles[0].Id.Equals(id))
                    return 1;

                return 0;
            });

            VehicleService = new VehicleService(Repository);
        }

        [Test]
        public async Task MustRegisterVehicle()
        {
            await VehicleService.RegisterVehicle(VehicleToRegister);

            await Repository.ReceivedWithAnyArgs(1).GetVehicle(license_plate: VehicleToRegister.LicensePlate);
            await Repository.ReceivedWithAnyArgs(1).RegisterVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustNotRegisterVehicleIfAlreadyExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.RegisterVehicle(ExistingVehicleDto));

            await Repository.ReceivedWithAnyArgs(1).GetVehicle(license_plate: ExistingVehicleDto.LicensePlate);
            await Repository.ReceivedWithAnyArgs(0).RegisterVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailToRegisterVehicle()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.RegisterVehicle(VehicleToFailRegister));

            await Repository.ReceivedWithAnyArgs(1).GetVehicle(license_plate: VehicleToFailRegister.LicensePlate);
            await Repository.ReceivedWithAnyArgs(1).RegisterVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustGetVehicles()
        {
            var vehicles = (await VehicleService.GetVehicles()).ToList();

            await Repository.Received(1).GetVehicles();
            Assert.That(vehicles, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(vehicles[0].Brand, Is.EqualTo(ExistingVehicle.Brand));
                Assert.That(vehicles[0].Model, Is.EqualTo(ExistingVehicle.Model));
                Assert.That(vehicles[0].Year, Is.EqualTo(ExistingVehicle.Year));
                Assert.That(vehicles[0].LicensePlate, Is.EqualTo(ExistingVehicle.LicensePlate.License));
            });

            Assert.Multiple(() =>
            {
                Assert.That(vehicles[1].Brand, Is.EqualTo(ExistingVehicle2.Brand));
                Assert.That(vehicles[1].Model, Is.EqualTo(ExistingVehicle2.Model));
                Assert.That(vehicles[1].Year, Is.EqualTo(ExistingVehicle2.Year));
                Assert.That(vehicles[1].LicensePlate, Is.EqualTo(ExistingVehicle2.LicensePlate.License));
            });
        }

        [Test]
        public async Task MustGetVehiclesWithSameLicensePlate()
        {
            var vehicles = (await VehicleService.GetVehicles(licensePlate: ExistingVehicle.LicensePlate.License)).ToList();

            await Repository.Received(1).GetVehicles(license_plate: ExistingVehicle.LicensePlate.License);
            Assert.That(vehicles, Has.Count.EqualTo(1));

            Assert.Multiple(() =>
            {
                Assert.That(vehicles[0].Brand, Is.EqualTo(ExistingVehicle.Brand));
                Assert.That(vehicles[0].Model, Is.EqualTo(ExistingVehicle.Model));
                Assert.That(vehicles[0].Year, Is.EqualTo(ExistingVehicle.Year));
                Assert.That(vehicles[0].LicensePlate, Is.EqualTo(ExistingVehicle.LicensePlate.License));
            });
        }

        [Test]
        public async Task MustGetVehicle()
        {
            var vehicle = await VehicleService.GetVehicle(licensePlate: ExistingVehicle.LicensePlate.License);

            await Repository.Received(1).GetVehicle(license_plate: ExistingVehicle.LicensePlate.License);
            Assert.That(vehicle, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(vehicle.Brand, Is.EqualTo(ExistingVehicle.Brand));
                Assert.That(vehicle.Model, Is.EqualTo(ExistingVehicle.Model));
                Assert.That(vehicle.Year, Is.EqualTo(ExistingVehicle.Year));
                Assert.That(vehicle.LicensePlate, Is.EqualTo(ExistingVehicle.LicensePlate.License));
            });
        }

        [Test]
        public async Task MustNotGetVehicleIfNotExists()
        {
            var vehicle = await VehicleService.GetVehicle(licensePlate: VehicleToRegister.LicensePlate);

            await Repository.Received(1).GetVehicle(license_plate: VehicleToRegister.LicensePlate);
            Assert.That(vehicle, Is.Null);
        }

        [Test]
        public async Task MustUpdateVehicle()
        {
            await VehicleService.UpdateVehicle(ExistingVehicleId, ExistingVehicleToUpdate);

            await Repository.Received(1).GetVehicle(id: ExistingVehicleId);
            await Repository.ReceivedWithAnyArgs(1).UpdateVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustNotUpdateVehicleIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.UpdateVehicle(Guid.NewGuid(), VehicleToRegister));

            await Repository.Received(1).GetVehicle(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).UpdateVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailtToUpdateVehicle()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.UpdateVehicle(ExistingVehicleId, ExistingVehicleToFailUpdateOrDelete));

            await Repository.Received(1).GetVehicle(ExistingVehicleId);
            await Repository.ReceivedWithAnyArgs(1).UpdateVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustDeleteVehicle()
        {
            await VehicleService.DeleteVehicle(ExistingVehicle.Id);

            await Repository.Received(1).GetVehicle(ExistingVehicle.Id);
            await Repository.ReceivedWithAnyArgs(1).DeleteVehicle(ExistingVehicle.Id);
        }

        [Test]
        public async Task MustNotDeleteVehicleIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.DeleteVehicle(Guid.NewGuid()));

            await Repository.Received(1).GetVehicle(Arg.Any<Guid>());
            await Repository.ReceivedWithAnyArgs(0).DeleteVehicle(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailtToDeleteVehicle()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.DeleteVehicle(ExistingVehicle2Id));

            await Repository.Received(1).GetVehicle(ExistingVehicle2Id);
            await Repository.ReceivedWithAnyArgs(1).DeleteVehicle(ExistingVehicle2.Id);
        }
    }
}
