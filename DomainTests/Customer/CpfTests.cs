using Domain.Customer;

namespace DomainTests.Customer
{
    public class CpfTests
    {
        [Test]
        public void MustNotCreateCpfIfNullOrWhiteSpace()
        {
            Assert.Catch<ArgumentNullException>(() => new Cpf(""));
            Assert.Catch<ArgumentNullException>(() => new Cpf(" "));

        }

        [Test]
        public void MustNotCreateCpfIfContainsWhiteSpace()
        {
            Assert.Catch<ArgumentNullException>(() => new Cpf("123 456 789 12"));
        }

        [Test]
        public void MustNotCreateCpfIfContainsAnyLetter()
        {
            Assert.Catch<ArgumentException>(() => new Cpf("123A5678912"));
        }

        [Test]
        public void MustNotCreateCpfIfLessThan11Digits()
        {
            Assert.Catch<ArgumentException>(() => new Cpf("1234567891"));
        }

        [Test]
        public void MustNotCreateCpfIfItIsInvalid()
        {
            Assert.Catch<ArgumentException>(() => new Cpf("12345678912"));
        }

        [Test]
        public void MustCreateCpf()
        {
            Cpf cpf = new("662.119.730-63");

            Assert.That(cpf, Is.Not.Null);
            Assert.That(cpf.Id, Is.Not.Null);
            Assert.That(cpf.Id, Is.Not.Empty);
        }

        [Test]
        public void MustCreateCpfAndNormalize()
        {
            Cpf cpf = new("66211973063");

            Assert.That(cpf, Is.Not.Null);
            Assert.That(cpf.Id, Is.EqualTo("662.119.730-63"));
        }
    }
}
