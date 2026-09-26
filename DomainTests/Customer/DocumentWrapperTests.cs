using Domain.Interface.Exceptions;
using Domain.Customer;

namespace DomainTests.Customer
{
    public class DocumentWrapperTests
    {
        [TestCase("52998224725", typeof(Cpf), "529.982.247-25")]
        [TestCase("529.982.247-25", typeof(Cpf), "529.982.247-25")]
        [TestCase("10359666000194", typeof(Cnpj), "10.359.666/0001-94")]
        [TestCase("10.359.666/0001-94", typeof(Cnpj), "10.359.666/0001-94")]
        public void MustUseSharedDocumentRules(string input, Type type, string expected)
        {
            var document = DocumentWrapper.CreateDocument(input);
            Assert.That(document, Is.TypeOf(type));
            Assert.That(document.Id, Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("52998224724")]
        [TestCase("10359666000195")]
        [TestCase("x52998224725")]
        [TestCase("x10359666000194")]
        [TestCase("529 98224725")]
        [TestCase("10359666 000194")]
        public void MustTranslateSharedValidationErrors(string? input) =>
            Assert.Throws<DomainValidationException>(() => DocumentWrapper.CreateDocument(input!));

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

