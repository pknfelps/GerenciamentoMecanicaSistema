using Domain.Interface.Custumer;
using Domain.Interface.Vehicle;
using Domain.Vehicle;
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

        private static readonly CreateVehicleDto VehicleToRegister = new("12345678912", "Fiat", "Mobi", 2025, "FIT4M08");
        private static readonly CreateVehicleDto VehicleToFailRegister = new("12345678912", "Test", "Test", 0000, "TST1234");

        private static readonly List<Vehicle> Vehicles = 
        [
            Substitute.For<Vehicle>(Substitute.For<IDocument>(), "Honda", "Civic", 2024, "CVC2024"),
            Substitute.For<Vehicle>(Substitute.For<IDocument>(), "Ford", "Ka", 2020, "FKA0F20")
        ];

        private static readonly VehicleDto ExistingVehicleDto = new(Guid.NewGuid(), "12345678912", "Honda", "Civic", 2024, "CVC2024");
        private static readonly VehicleDto ExistingVehicleToUpdate = new(Guid.NewGuid(), "12345678912", "Honda", "City", 2020, "CVC2024");
        private static readonly VehicleDto ExistingVehicleToFailUpdateOrDelete = new(Guid.NewGuid(), "12345678912", "Test", "Test", 2020, "FKA0F20");

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

            Repository.GetVehicles().Returns(Vehicles);

            Repository.GetVehicle(Arg.Any<string>()).Returns(callInfo =>
            {
                var license = callInfo.ArgAt<string>(0);

                return Vehicles.FirstOrDefault(x => x.LicensePlate.License.Equals(license));
            });

            Repository.UpdateVehicle(Arg.Any<IVehicle>()).Returns(callInfo =>
            {
                var vehicle = callInfo.ArgAt<IVehicle>(0);

                if (vehicle.Brand == VehicleToFailRegister.Brand && vehicle.Model == VehicleToFailRegister.Model)
                    return 0;

                if (Vehicles.FirstOrDefault(x => x.LicensePlate.License.Equals(vehicle.LicensePlate.License)) != default)
                    return 1;

                return 0;
            });

            Repository.DeleteVehicle(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (Vehicles[0].Id.Equals(id))
                    return 1;

                return 0;
            });

            VehicleService = new VehicleService(Repository);
        }

        [Test]
        public async Task MustRegisterVehicle()
        {
            await VehicleService.RegisterVehicle(VehicleToRegister);

            await Repository.ReceivedWithAnyArgs(1).GetVehicle(VehicleToRegister.LicensePlate);
            await Repository.ReceivedWithAnyArgs(1).RegisterVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustNotRegisterVehicleIfAlreadyExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.RegisterVehicle(ExistingVehicleDto));

            await Repository.ReceivedWithAnyArgs(1).GetVehicle(ExistingVehicleDto.LicensePlate);
            await Repository.ReceivedWithAnyArgs(0).RegisterVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailToRegisterVehicle()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.RegisterVehicle(VehicleToFailRegister));

            await Repository.ReceivedWithAnyArgs(1).GetVehicle(VehicleToFailRegister.LicensePlate);
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
                Assert.That(vehicles[0].Brand, Is.EqualTo(Vehicles[0].Brand));
                Assert.That(vehicles[0].Model, Is.EqualTo(Vehicles[0].Model));
                Assert.That(vehicles[0].Year, Is.EqualTo(Vehicles[0].Year));
                Assert.That(vehicles[0].LicensePlate, Is.EqualTo(Vehicles[0].LicensePlate.License));
            });

            Assert.Multiple(() =>
            {
                Assert.That(vehicles[1].Brand, Is.EqualTo(Vehicles[1].Brand));
                Assert.That(vehicles[1].Model, Is.EqualTo(Vehicles[1].Model));
                Assert.That(vehicles[1].Year, Is.EqualTo(Vehicles[1].Year));
                Assert.That(vehicles[1].LicensePlate, Is.EqualTo(Vehicles[1].LicensePlate.License));
            });
        }

        [Test]
        public async Task MustGetVehicle()
        {
            var vehicle = await VehicleService.GetVehicle(Vehicles[0].LicensePlate.License);

            await Repository.Received(1).GetVehicle(Vehicles[0].LicensePlate.License);
            Assert.That(vehicle, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(vehicle.Brand, Is.EqualTo(Vehicles[0].Brand));
                Assert.That(vehicle.Model, Is.EqualTo(Vehicles[0].Model));
                Assert.That(vehicle.Year, Is.EqualTo(Vehicles[0].Year));
                Assert.That(vehicle.LicensePlate, Is.EqualTo(Vehicles[0].LicensePlate.License));
            });
        }

        [Test]
        public async Task MustNotGetVehicleIfNotExists()
        {
            var vehicle = await VehicleService.GetVehicle(VehicleToRegister.LicensePlate);

            await Repository.Received(1).GetVehicle(VehicleToRegister.LicensePlate);
            Assert.That(vehicle, Is.Null);
        }

        [Test]
        public async Task MustUpdateVehicle()
        {
            await VehicleService.UpdateVehicle(ExistingVehicleToUpdate);

            await Repository.Received(1).GetVehicle(ExistingVehicleToUpdate.LicensePlate);
            await Repository.ReceivedWithAnyArgs(1).UpdateVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustNotUpdateVehicleIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.UpdateVehicle(Arg.Any<VehicleDto>()));

            await Repository.Received(1).GetVehicle(VehicleToRegister.LicensePlate);
            await Repository.ReceivedWithAnyArgs(0).UpdateVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailtToUpdateVehicle()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.UpdateVehicle(ExistingVehicleToFailUpdateOrDelete));

            await Repository.Received(1).GetVehicle(ExistingVehicleToFailUpdateOrDelete.LicensePlate);
            await Repository.ReceivedWithAnyArgs(1).UpdateVehicle(Arg.Any<IVehicle>());
        }

        [Test]
        public async Task MustDeleteVehicle()
        {
            await VehicleService.DeleteVehicle(Vehicles[0].LicensePlate.License);

            await Repository.Received(1).GetVehicle(Vehicles[0].LicensePlate.License);
            await Repository.ReceivedWithAnyArgs(1).DeleteVehicle(Vehicles[0].Id);
        }

        [Test]
        public async Task MustNotDeleteVehicleIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.DeleteVehicle(VehicleToRegister.LicensePlate));

            await Repository.Received(1).GetVehicle(VehicleToRegister.LicensePlate);
            await Repository.ReceivedWithAnyArgs(0).DeleteVehicle(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailtToDeleteVehicle()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await VehicleService.DeleteVehicle(ExistingVehicleToFailUpdateOrDelete.LicensePlate));

            await Repository.Received(1).GetVehicle(ExistingVehicleToFailUpdateOrDelete.LicensePlate);
            await Repository.ReceivedWithAnyArgs(1).DeleteVehicle(Vehicles[1].Id);
        }
    }
}
