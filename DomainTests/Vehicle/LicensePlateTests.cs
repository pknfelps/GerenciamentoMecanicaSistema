using Domain.Vehicle;

namespace DomainTests.Vehicle
{
    public class LicensePlateTests
    {
        [Test]
        public void MustNotCreateLicensePlateIfNotValid()
        {
            Assert.Throws<ArgumentException>(() => LicensePlateWrapper.CreateLicensePlate("PL123"));
    }
}
}
