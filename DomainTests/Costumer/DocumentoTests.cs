using Domain.Costumer;

namespace DomainTests.Costumer
{
    public class DocumentoTests
    {
        [Test]
        public void MustNoCreateDocumentoIfNotValid()
        {
            Assert.Catch<ArgumentException>(() => DocumentWrapper.CreateDocument("1234"));
        }
    }
}
