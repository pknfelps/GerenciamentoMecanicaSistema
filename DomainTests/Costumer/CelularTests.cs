using Domain.Costumer;

namespace DomainTests.Costumer
{
    public class CelularTests
    {
        [Test]
        public void MustNotCreateCelularIfNumberIsNullOrWhiteSpace()
        {
            Assert.Catch<ArgumentNullException>(() => new Celular(""));
            Assert.Catch<ArgumentNullException>(() => new Celular(" "));
        }

        [Test]
        public void MustNotCreateCelularIfNumberContainsLetters()
        {
            Assert.Catch<ArgumentException>(() => new Celular("1191234567a"));
        }

        [Test]
        public void MustNotCreateCelularWithLessThan11Digits()
        {
            Assert.Catch<ArgumentException>(() => new Celular("1191234567"));
        }

        [Test]
        public void MustCreateCelular()
        {
            Celular celular = new("11 91234-5678");

            Assert.That(celular, Is.Not.Null);
            Assert.That(celular.Numero, Is.Not.Null);
            Assert.That(celular.Numero, Is.Not.Empty);
        }

        [Test]
        public void MustCreateCelularAndNormalizeNumber()
        {
            Celular celular = new("11 91234-5678");

            Assert.That(celular, Is.Not.Null);
            Assert.That(celular.Numero, Is.EqualTo("(11) 91234-5678"));
        }
    }
}
