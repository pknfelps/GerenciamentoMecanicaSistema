using NSubstitute;
using Service.Interface;
using Service.Interface.Dto.Vehicle;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class VehicleControllerTests : BaseControllerTests
    {
        private IVehicleService VehicleService { get; set; }

        private static readonly CreateVehicleDto VehicleToRegister = new("123.456.789-12", "Fiat", "Mobi", 2025, "FIT4M08");

        private static Guid ExistingVehicleId = Guid.NewGuid();

        private static readonly List<VehicleDto> Vehicles =
        [
            new(ExistingVehicleId, "123.456.789-12", "Honda", "Civic", 2024, "CVC2024"),
            new(Guid.NewGuid(), "123.456.789-12", "Ford", "Ka", 2020, "FKA0F20")
        ];
        private static readonly VehicleDto VehicleToUpdate = new(ExistingVehicleId, "123.456.789-12", "Honda", "City", 2020, "CVC2024");

        protected override void MockService()
        {
            VehicleService = TestWebAppFactory.VehicleServiceMock;

            VehicleService.RegisterVehicle(Arg.Any<CreateVehicleDto>()).Returns(callInfo =>
            {
                var vehicle = callInfo.ArgAt<CreateVehicleDto>(0);

                if (vehicle.Equals(VehicleToRegister))
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            VehicleService.GetVehicles().Returns(Vehicles);

            VehicleService.GetVehicle(Arg.Any<string>()).Returns(callInfo =>
            {
                var license = callInfo.ArgAt<string>(0);

                return Vehicles.FirstOrDefault(x => x.LicensePlate.Equals(license));
            });

            VehicleService.UpdateVehicle(Arg.Any<VehicleDto>()).Returns(callInfo =>
            {
                var vehicle = callInfo.ArgAt<VehicleDto>(0);

                if (Vehicles.FirstOrDefault(x => x.LicensePlate.Equals(vehicle.LicensePlate)) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });

            VehicleService.DeleteVehicle(Arg.Any<string>()).Returns(callInfo =>
            {
                var license = callInfo.ArgAt<string>(0);

                if (Vehicles.FirstOrDefault(x => x.LicensePlate.Equals(license)) != default)
                    return Task.CompletedTask;

                throw new InvalidOperationException();
            });
        }

        [Test]
        public async Task MustRegisterVehicle()
        {
            var response = await TestClient.PostAsJsonAsync("/Vehicle/RegisterVehicle", VehicleToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await VehicleService.Received(1).RegisterVehicle(VehicleToRegister);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRegisterVehicleWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("/Vehicle/RegisterVehicle", new { Nome = "Honda" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await VehicleService.Received(0).RegisterVehicle(Arg.Any<CreateVehicleDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfFailRegisterVehicle()
        {
            var response = await TestClient.PostAsJsonAsync("/Vehicle/RegisterVehicle", Vehicles[0]);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await VehicleService.Received(1).RegisterVehicle(Vehicles[0]);
        }

        [Test]
        public async Task MustGetVehicles()
        {
            var response = await TestClient.GetAsync("/Vehicle/GetVehicles");
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<CreateVehicleDto>>();
            var vehicles = result?.ToList();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            Assert.Multiple(() =>
            {
                Assert.That(vehicles, Is.Not.Null);
                Assert.That(vehicles, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(vehicles[0].Equals(Vehicles[0]), Is.True);
                Assert.That(vehicles[1].Equals(Vehicles[1]), Is.True);
            });

            await VehicleService.Received(1).GetVehicles();
        }

        [Test]
        public async Task MustGetVehicle()
        {
            var response = await TestClient.GetAsync($"/Vehicle/GetVehicle/{Vehicles[0].LicensePlate}");
            var vehicle = await response.Content.ReadFromJsonAsync<CreateVehicleDto>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            Assert.Multiple(() =>
            {
                Assert.That(vehicle, Is.Not.Null);
                Assert.That(vehicle.Equals(Vehicles[0]), Is.True);
            });

            await VehicleService.Received(1).GetVehicle(Vehicles[0].LicensePlate);
        }

        [Test]
        public async Task MustReturnNotFoundIfTryGetVehicleThatNotExists()
        {
            var response = await TestClient.GetAsync($"/Vehicle/GetVehicle/TST1234");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await VehicleService.Received(1).GetVehicle("TST1234");
        }

        [Test]
        public async Task MustReturnBadRequestIfTryGetVehicleWithInvalidModel()
        {
            var response = await TestClient.GetAsync("/Vehicle/GetVehicle/ÇÁG1234");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await VehicleService.Received(0).GetVehicle(Arg.Any<string>());
        }

        [Test]
        public async Task MustUpdateVehicle()
        {
            var response = await TestClient.PatchAsJsonAsync("/Vehicle/UpdateVehicle", VehicleToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await VehicleService.Received(1).UpdateVehicle(VehicleToUpdate);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateVehicleWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync("/Vehicle/UpdateVehicle", new { Nome = "Honda" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await VehicleService.Received(0).UpdateVehicle(Arg.Any<VehicleDto>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfFailUpdateVehicle()
        {
            var vehicle = new VehicleDto(Guid.NewGuid(), VehicleToRegister.CustomerDocument, VehicleToRegister.Brand, VehicleToRegister.Model, VehicleToRegister.Year, VehicleToRegister.LicensePlate);

            var response = await TestClient.PatchAsJsonAsync("/Vehicle/UpdateVehicle", vehicle);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await VehicleService.Received(1).UpdateVehicle(vehicle);
        }

        [Test]
        public async Task MustDeleteVehicle()
        {
            var response = await TestClient.DeleteAsync($"/Vehicle/DeleteVehicle/{Vehicles[0].LicensePlate}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await VehicleService.Received(1).DeleteVehicle(Vehicles[0].LicensePlate);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteVehicleWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync("/Vehicle/DeleteVehicle/ÇÁG1234");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await VehicleService.Received(0).DeleteVehicle(Arg.Any<string>());
        }

        [Test]
        public async Task MustReturnInternalServerErrorIfFailDeleteVehicle()
        {
            var response = await TestClient.DeleteAsync($"/Vehicle/DeleteVehicle/{VehicleToRegister.LicensePlate}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            await VehicleService.Received(1).DeleteVehicle(VehicleToRegister.LicensePlate);
        }
    }
}
