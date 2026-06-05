using Domain.Vehicle;

namespace DomainTests.Vehicle
{
    public class MercosulLicensePlateTests
    {
        [Test]
        public void MustCreateLicensePlate()
        {
            var license = new MercosulLicensePlate("POR5H33");

            Assert.That(license, Is.Not.Null);
            Assert.That(license.License, Is.EqualTo("POR5H33"));
        }

        [Test]
        public void MustCreateLicensePlateByWrapper()
        {
            var license = LicensePlateWrapper.CreateLicensePlate("POR5H33");

            Assert.That(license, Is.Not.Null);
            Assert.That(license.License, Is.EqualTo("POR5H33"));
        }

        [Test]
        public void MustNotCreateLicensePlateIfNotContainsExectedLenght()
        {
            Assert.Throws<ArgumentException>(() => new MercosulLicensePlate("PO58"));
        }

        [Test]
        public void MustNotCreateLicensePlateIfContainsSymbolOrPunctuation()
        {
            Assert.Throws<ArgumentException>(() => new MercosulLicensePlate("POR$.33"));
        }

        [Test]
        public void MustNotCreateLicensePlateIfIsNotAValidModel()
        {
            Assert.Throws<ArgumentException>(() => new MercosulLicensePlate("POR533H"));
        }
    }
}
