using Domain.Customer;

namespace DomainTests.Customer
{
    public class CnpjTests
    {
        [Test]
        public void MustNotCreateCnpjIfNullOrWhiteSpace()
        {
            Assert.Catch<ArgumentNullException>(() => new Cnpj(""));
            Assert.Catch<ArgumentNullException>(() => new Cnpj(" "));
        }

        [Test]
        public void MustNotCreateCnpjIfContainsWhiteSpace()
        {
            Assert.Catch<ArgumentNullException>(() => new Cnpj("12 345 678 0001 01"));
        }

        [Test]
        public void MustNotCreateCnpjIfContainsAnyLetter()
        {
            Assert.Catch<ArgumentException>(() => new Cnpj("12345678O00101"));
        }

        [Test]
        public void MustNotCreateCnpjIfLessThan14Digits()
        {
            Assert.Catch<ArgumentException>(() => new Cnpj("1234567800010"));
        }

        [Test]
        public void MustNotCreateCnpjIfItIsInvalid()
        {
            Assert.Catch<ArgumentException>(() => new Cnpj("12123456000100"));
        }

        [Test]
        public void MustCreateCnpj()
        {
            Cnpj cnpj = new("10.359.666/0001-94");

            Assert.That(cnpj, Is.Not.Null);
            Assert.That(cnpj.Id, Is.Not.Null);
            Assert.That(cnpj.Id, Is.Not.Empty);
        }

        [Test]
        public void MustCreateCnpjAndNormalize()
        {
            Cnpj cpf = new("10359666000194");

            Assert.That(cpf, Is.Not.Null);
            Assert.That(cpf.Id, Is.EqualTo("10.359.666/0001-94"));
        }
    }
}
