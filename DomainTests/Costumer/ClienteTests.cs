using Domain.Costumer;

namespace DomainTests.Costumer
{
    public class ClienteTests
    {
        [Test]
        public void MustCreateCliente()
        {
            Cliente cliente = new("Fulano", "123.456.789-12", "11 91234-5678", "fulano@gmail.com");

            Assert.That(cliente, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Nome, Is.Not.Null);
                Assert.That(cliente.Nome, Is.EqualTo("Fulano"));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Documento, Is.Not.Null);
                Assert.That(cliente.Documento.Id, Is.EqualTo("123.456.789-12"));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Celular, Is.Not.Null);
                Assert.That(cliente.Celular.Numero, Is.EqualTo("(11) 91234-5678"));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Email, Is.Not.Null);
                Assert.That(cliente.Email.Endereco, Is.EqualTo("fulano@gmail.com"));
            });

            Assert.That(cliente.Id, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void MustNotCreateClienteIfNomeIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => new Cliente("", "123.456.789-12", "11 91234-5678", "fulano@gmail.com"));
            Assert.Throws<ArgumentNullException>(() => new Cliente(" ", "123.456.789-12", "11 91234-5678", "fulano@gmail.com"));
        }
    }
}
