using Domain.Interface.Exceptions;
namespace DomainTests.Vehicle
{
    public class VehicleTests
    {
        private string CostumerDocument { get; set; } = "662.119.730-63";

        [Test]
        public void MustCreateVehicleWithoutId()
        {
            var vehicle = new Domain.Vehicle.Vehicle(CostumerDocument, "Porsche", "911", 1990, "PRX3911");

            Assert.That(vehicle, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(vehicle.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(vehicle.Brand, Is.EqualTo("Porsche"));
                Assert.That(vehicle.Model, Is.EqualTo("911"));
                Assert.That(vehicle.Year, Is.EqualTo(1990));
                Assert.That(vehicle.LicensePlate.License, Is.EqualTo("PRX3911"));
            });
        }

        [Test]
        public void MustCreateVehicleWithId()
        {
            var id = Guid.NewGuid();

            var vehicle = new Domain.Vehicle.Vehicle(id, CostumerDocument, "Porsche", "911", 1990, "PRX3911");

            Assert.That(vehicle, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(vehicle.Id, Is.EqualTo(id));
                Assert.That(vehicle.Brand, Is.EqualTo("Porsche"));
                Assert.That(vehicle.Model, Is.EqualTo("911"));
                Assert.That(vehicle.Year, Is.EqualTo(1990));
                Assert.That(vehicle.LicensePlate.License, Is.EqualTo("PRX3911"));
            });
        }

        [Test]
        public void MustNotCreateVehicleIdIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.Vehicle.Vehicle(Guid.Empty, CostumerDocument, "Porsche", "911", 1990, "PRX3911"));
        }

        [Test]
        public void MustNotCreateVehicleIfCostumerDocumentIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.Vehicle.Vehicle("", "Porsche", "911", 1990, "PRX3911"));
        }

        [Test]
        public void MustNotCreateVehicleIfBrandIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.Vehicle.Vehicle(CostumerDocument, "", "911", 1990, "PRX3911"));
        }

        [Test]
        public void MustNotCreateVehicleIfModelIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.Vehicle.Vehicle(CostumerDocument, "Porsche", "", 1990, "PRX3911"));
        }

        [Test]
        public void MustNotCreateVehicleIfYearIsLowerThan0()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.Vehicle.Vehicle(CostumerDocument, "Porsche", "911", -1, "PRX3911"));
        }

        [Test]
        public void MustNotCreateVehicleIfLicenselIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.Vehicle.Vehicle(CostumerDocument, "Porsche", "911", 1990, ""));
        }
    }
}

