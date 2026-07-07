using Domain.Interface.Exceptions;
using Domain.Customer;

namespace DomainTests.Customer
{
    public class DocumentWrapperTests
    {
        [Test]
        public void MustNoCreateDocumentIfNotValid()
        {
            Assert.Catch<DomainValidationException>(() => DocumentWrapper.CreateDocument("1234"));
        }

        [Test]
        public void MustCreateCnpjByWrapper()
        {
            Document cnpj = DocumentWrapper.CreateDocument("10.359.666/0001-94");

            Assert.That(cnpj, Is.Not.Null);
            Assert.That(cnpj, Is.TypeOf<Cnpj>());
        }

        [Test]
        public void MustCreateCpfByWrapper()
        {
            Document cpf = DocumentWrapper.CreateDocument("662.119.730-63");

            Assert.That(cpf, Is.Not.Null);
            Assert.That(cpf, Is.TypeOf<Cpf>());
        }
    }
}

