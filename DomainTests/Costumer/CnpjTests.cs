using Domain.Costumer;

namespace DomainTests.Costumer
{
    public class CnpjTests
    {
        [Test]
        public void MustNotCreateCnpjIfNullOrWhiteSpace()
        {
            Assert.Catch<ArgumentNullException>(() => new Cnpj(""));
            Assert.Catch<ArgumentNullException>(() => new Cnpj(" "));
            Assert.Catch<ArgumentNullException>(() => new Cnpj(null));
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
        public void MustCreateCnpj()
        {
            Documento cnpj = DocumentWrapper.CreateDocument("12.345.678/0001-01");

            Assert.That(cnpj, Is.Not.Null);
            Assert.That(cnpj.Id, Is.Not.Null);
            Assert.That(cnpj.Id, Is.Not.Empty);
        }

        [Test]
        public void MustCreateCnpjByWrapper()
        {
            Documento cnpj = DocumentWrapper.CreateDocument("12.345.678/0001-01");

            Assert.That(cnpj, Is.Not.Null);
            Assert.That(cnpj, Is.TypeOf<Cnpj>());
        }

        [Test]
        public void MustCreateCnpjAndNormalize()
        {
            Cnpj cpf = new("12345678000101");

            Assert.That(cpf, Is.Not.Null);
            Assert.That(cpf.Id, Is.EqualTo("12.345.678/0001-01"));
        }
    }
}
