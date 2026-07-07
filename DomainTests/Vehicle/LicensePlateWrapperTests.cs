using Domain.Interface.Exceptions;
using Domain.Vehicle;

namespace DomainTests.Vehicle
{
    public class LicensePlateWrapperTests
    {
        [Test]
        public void MustNotCreateLicensePlateIfNotValid()
        {
            Assert.Catch<DomainValidationException>(() => LicensePlateWrapper.CreateLicensePlate("PL123"));
        }

        [Test]
        public void MustCreateMercosulLicensePlateByWrapper()
        {
            var license = LicensePlateWrapper.CreateLicensePlate("POR5H33");

            Assert.That(license, Is.Not.Null);
            Assert.That(license.License, Is.EqualTo("POR5H33"));
            Assert.That(license, Is.TypeOf<MercosulLicensePlate>());
        }

        [Test]
        public void MustCreateOldLicensePlateByWrapper()
        {
            var license = LicensePlateWrapper.CreateLicensePlate("PRX3911");

            Assert.That(license, Is.Not.Null);
            Assert.That(license.License, Is.EqualTo("PRX3911"));
            Assert.That(license, Is.TypeOf<OldBrazilLicensePlate>());
        }
    }
}

