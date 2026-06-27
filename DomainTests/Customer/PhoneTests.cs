using Domain.Customer;

namespace DomainTests.Customer
{
    public class PhoneTests
    {
        [Test]
        public void MustNotCreatePhoneIfNumberIsNullOrWhiteSpace()
        {
            Assert.Catch<ArgumentNullException>(() => new Phone(""));
            Assert.Catch<ArgumentNullException>(() => new Phone(" "));
        }

        [Test]
        public void MustNotCreatePhoneIfNumberContainsLetters()
        {
            Assert.Catch<ArgumentException>(() => new Phone("1191234567a"));
        }

        [Test]
        public void MustNotCreatePhoneWithLessThan11Digits()
        {
            Assert.Catch<ArgumentException>(() => new Phone("1191234567"));
        }

        [Test]
        public void MustCreatePhone()
        {
            Phone celular = new("11 91234-5678");

            Assert.That(celular, Is.Not.Null);
            Assert.That(celular.Number, Is.Not.Null);
            Assert.That(celular.Number, Is.Not.Empty);
        }

        [Test]
        public void MustCreatePhoneAndNormalizeNumber()
        {
            Phone celular = new("11 91234-5678");

            Assert.That(celular, Is.Not.Null);
            Assert.That(celular.Number, Is.EqualTo("(11) 91234-5678"));
        }
    }
}
