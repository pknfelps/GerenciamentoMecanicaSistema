using Domain.Customer;

namespace DomainTests.Costumer
{
    public class ClienteTests
    {
        [Test]
        public void MustCreateClienteWithoutId()
        {
            Customer cliente = new("Fulano", "123.456.789-12", "11 91234-5678", "fulano@gmail.com");

            Assert.That(cliente, Is.Not.Null);

            Assert.That(cliente.Id, Is.Not.EqualTo(Guid.Empty));

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Name, Is.Not.Null);
                Assert.That(cliente.Name, Is.EqualTo("Fulano"));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Document, Is.Not.Null);
                Assert.That(cliente.Document.Id, Is.EqualTo("123.456.789-12"));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Phone, Is.Not.Null);
                Assert.That(cliente.Phone.Number, Is.EqualTo("(11) 91234-5678"));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Email, Is.Not.Null);
                Assert.That(cliente.Email.Address, Is.EqualTo("fulano@gmail.com"));
            });
        }

        [Test]
        public void MustCreateClienteWithId()
        {
            Guid clienteId = Guid.NewGuid();

            Customer cliente = new(clienteId, "Fulano", "123.456.789-12", "11 91234-5678", "fulano@gmail.com");

            Assert.That(cliente, Is.Not.Null);

            Assert.That(cliente.Id, Is.EqualTo(clienteId));

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Name, Is.Not.Null);
                Assert.That(cliente.Name, Is.EqualTo("Fulano"));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Document, Is.Not.Null);
                Assert.That(cliente.Document.Id, Is.EqualTo("123.456.789-12"));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Phone, Is.Not.Null);
                Assert.That(cliente.Phone.Number, Is.EqualTo("(11) 91234-5678"));
            });

            Assert.Multiple(() =>
            {
                Assert.That(cliente.Email, Is.Not.Null);
                Assert.That(cliente.Email.Address, Is.EqualTo("fulano@gmail.com"));
            });
        }

        [Test]
        public void MustNotCreateClienteIfNomeIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => new Customer("", "123.456.789-12", "11 91234-5678", "fulano@gmail.com"));
            Assert.Throws<ArgumentNullException>(() => new Customer(" ", "123.456.789-12", "11 91234-5678", "fulano@gmail.com"));
        }
    }
}
