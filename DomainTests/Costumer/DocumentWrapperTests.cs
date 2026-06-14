using Domain.Customer;

namespace DomainTests.Costumer
{
    public class DocumentWrapperTests
    {
        [Test]
        public void MustNoCreateDocumentIfNotValid()
        {
            Assert.Catch<ArgumentException>(() => DocumentWrapper.CreateDocument("1234"));
        }

        [Test]
        public void MustCreateCnpjByWrapper()
        {
            Document cnpj = DocumentWrapper.CreateDocument("12.345.678/0001-01");

            Assert.That(cnpj, Is.Not.Null);
            Assert.That(cnpj, Is.TypeOf<Cnpj>());
        }

        [Test]
        public void MustCreateCpfByWrapper()
        {
            Document cpf = DocumentWrapper.CreateDocument("123.456.789-12");

            Assert.That(cpf, Is.Not.Null);
            Assert.That(cpf, Is.TypeOf<Cpf>());
        }
    }
}
