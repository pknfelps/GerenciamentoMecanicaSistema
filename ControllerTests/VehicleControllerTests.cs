using GerenciamentoMecanicaSistema.Contracts.Requests.Vehicle;
using GerenciamentoMecanicaSistema.Contracts.Responses.Vehicle;
using NSubstitute;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Vehicle;
using Service.Interface.Results.Vehicle;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests
{
    public class VehicleControllerTests : BaseControllerTests
    {
        private IVehicleService VehicleService { get; set; }

        private static readonly CreateVehicleRequest VehicleToRegister = new("123.456.789-12", "Fiat", "Mobi", 2025, "FIT4M08");

        private static Guid ExistingVehicleId = Guid.NewGuid();

        private static readonly List<VehicleResult> Vehicles =
        [
            new(ExistingVehicleId, "123.456.789-12", "Honda", "Civic", 2024, "CVC2024"),
            new(Guid.NewGuid(), "123.456.789-12", "Ford", "Ka", 2020, "FKA0F20")
        ];
        private static readonly CreateVehicleRequest VehicleToUpdate = new("123.456.789-12", "Honda", "City", 2020, "CVC2024");

        protected override void MockService()
        {
            VehicleService = TestWebAppFactory.VehicleServiceMock;

            VehicleService.RegisterVehicle(Arg.Any<CreateVehicleCommand>()).Returns(callInfo =>
            {
                var vehicle = callInfo.ArgAt<CreateVehicleCommand>(0);

                if (vehicle.Equals(VehicleToRegister.ToCommand()))
                    return Task.CompletedTask;

                throw new ConflictException("Conflito");
            });

            VehicleService.GetVehicles(licensePlate: Arg.Any<string>()).Returns(callInfo =>
            {
                var license = callInfo.ArgAt<string>(1);

                if (!string.IsNullOrEmpty(license))
                    return Vehicles.Where(x => x.LicensePlate == license);

                return Vehicles;
            });

            VehicleService.UpdateVehicle(Arg.Any<Guid>(), Arg.Any<CreateVehicleCommand>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (Vehicles.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });

            VehicleService.DeleteVehicle(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (Vehicles.FirstOrDefault(x => x.Id == id) != default)
                    return Task.CompletedTask;

                throw new NotFoundException("Recurso não encontrado");
            });
        }

        [Test]
        public async Task MustRegisterVehicle()
        {
            var response = await TestClient.PostAsJsonAsync("vehicles", VehicleToRegister);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            await VehicleService.Received(1).RegisterVehicle(VehicleToRegister.ToCommand());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryRegisterVehicleWithInvalidModel()
        {
            var response = await TestClient.PostAsJsonAsync("vehicles", new { Nome = "Honda" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await VehicleService.Received(0).RegisterVehicle(Arg.Any<CreateVehicleCommand>());
        }

        [Test]
        public async Task MustReturnConflictIfTryRegisterVehicleThatAlreadyExists()
        {
            var response = await TestClient.PostAsJsonAsync("vehicles", VehicleResponse.Create(Vehicles[0]));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

            await VehicleService.Received(1).RegisterVehicle(Arg.Any<CreateVehicleCommand>());
        }

        [Test]
        public async Task MustGetVehicles()
        {
            var response = await TestClient.GetAsync("vehicles");
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<VehicleResponse>>();
            var vehicles = result?.ToList();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            Assert.Multiple(() =>
            {
                Assert.That(vehicles, Is.Not.Null);
                Assert.That(vehicles, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(vehicles[0].Id, Is.EqualTo(Vehicles[0].Id));
                Assert.That(vehicles[0].LicensePlate, Is.EqualTo(Vehicles[0].LicensePlate));
                Assert.That(vehicles[1].Id, Is.EqualTo(Vehicles[1].Id));
                Assert.That(vehicles[1].LicensePlate, Is.EqualTo(Vehicles[1].LicensePlate));
            });

            await VehicleService.Received(1).GetVehicles();
        }

        [Test]
        public async Task MustGetVehicle()
        {
            var response = await TestClient.GetAsync($"vehicles?licensePlate={Vehicles[0].LicensePlate}");
            var vehicles = await response.Content.ReadFromJsonAsync<List<VehicleResponse>>();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(vehicles, Has.Count.EqualTo(1));

            var vehicle = vehicles[0];
            Assert.Multiple(() =>
            {
                Assert.That(vehicle, Is.Not.Null);
                Assert.That(vehicle.Id, Is.EqualTo(Vehicles[0].Id));
                Assert.That(vehicle.LicensePlate, Is.EqualTo(Vehicles[0].LicensePlate));
            });

            await VehicleService.Received(1).GetVehicles(licensePlate: Vehicles[0].LicensePlate);
        }

        [Test]
        public async Task MustUpdateVehicle()
        {
            var response = await TestClient.PatchAsJsonAsync($"vehicles/{ExistingVehicleId}", VehicleToUpdate);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            await VehicleService.Received(1).UpdateVehicle(ExistingVehicleId, VehicleToUpdate.ToCommand());
        }

        [Test]
        public async Task MustReturnBadRequestIfTryUpdateVehicleWithInvalidModel()
        {
            var response = await TestClient.PatchAsJsonAsync("vehicles/0000", new { Nome = "Honda" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await VehicleService.Received(0).UpdateVehicle(Arg.Any<Guid>(), Arg.Any<CreateVehicleCommand>());
        }

        [Test]
        public async Task MustReturnNotFoundIfTryUpdateVehicleThatNotExists()
        {
            var vehicle = new CreateVehicleRequest(VehicleToRegister.CustomerDocument, VehicleToRegister.Brand, VehicleToRegister.Model, VehicleToRegister.Year, VehicleToRegister.LicensePlate);

            var response = await TestClient.PatchAsJsonAsync($"vehicles/{Guid.NewGuid()}", vehicle);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await VehicleService.Received(1).UpdateVehicle(Arg.Any<Guid>(), vehicle.ToCommand());
        }

        [Test]
        public async Task MustDeleteVehicle()
        {
            var response = await TestClient.DeleteAsync($"vehicles/{Vehicles[0].Id}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await VehicleService.Received(1).DeleteVehicle(Vehicles[0].Id);
        }

        [Test]
        public async Task MustReturnBadRequestIfTryDeleteVehicleWithInvalidModel()
        {
            var response = await TestClient.DeleteAsync("vehicles/ÇÁG1234");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            await VehicleService.Received(0).DeleteVehicle(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustReturnNotFoundIfTryDeleteVehicleThatNotExists()
        {
            var response = await TestClient.DeleteAsync($"vehicles/{Guid.NewGuid()}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await VehicleService.Received(1).DeleteVehicle(Arg.Any<Guid>());
        }
    }
}