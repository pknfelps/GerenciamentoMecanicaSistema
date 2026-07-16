using Domain.Interface.Exceptions;
using Domain.Vehicle;

namespace DomainTests.Vehicle
{
    public class OldBrazilLicensePlateTests
    {
        [Test]
        public void MustCreateLicensePlate()
        {
            var license = new OldBrazilLicensePlate("PRX3911");

            Assert.That(license, Is.Not.Null);
            Assert.That(license.License, Is.EqualTo("PRX3911"));
        }

        [Test]
        public void MustNotCreateLicensePlateIfNotContainsExectedLenght()
        {
            Assert.Catch<DomainValidationException>(() => new OldBrazilLicensePlate("PR91"));
        }

        [Test]
        public void MustNotCreateLicensePlateIfContainsSymbolOrPunctuation()
        {
            Assert.Catch<DomainValidationException>(() => new OldBrazilLicensePlate("PR$.911"));
        }

        [Test]
        public void MustNotCreateLicensePlateIfIsNotAValidModel()
        {
            Assert.Catch<DomainValidationException>(() => new OldBrazilLicensePlate("PR3911X"));
        }
    }
}

