using Domain.Interface.Exceptions;
namespace DomainTests.Customer
{
    public class CustomerTests
    {
        [Test]
        public void MustCreateCustomerWithoutId()
        {
            Domain.Customer.Customer cliente = new("Fulano", "662.119.730-63", "11 91234-5678", "fulano@gmail.com");

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
                Assert.That(cliente.Document.Id, Is.EqualTo("662.119.730-63"));
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
        public void MustCreateCustomerWithId()
        {
            Guid clienteId = Guid.NewGuid();

            Domain.Customer.Customer cliente = new(clienteId, "Fulano", "662.119.730-63", "11 91234-5678", "fulano@gmail.com");

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
                Assert.That(cliente.Document.Id, Is.EqualTo("662.119.730-63"));
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
        public void MustNotCreateCustomerWithEmptyId()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.Customer.Customer(Guid.Empty, "Fulano", "662.119.730-63", "11 91234-5678", "fulano@gmail.com"));
        }

        [Test]
        public void MustNotCreateCustomerIfNomeIsEmpty()
        {
            Assert.Catch<DomainValidationException>(() => new Domain.Customer.Customer("", "123.456.789-12", "11 91234-5678", "fulano@gmail.com"));
            Assert.Catch<DomainValidationException>(() => new Domain.Customer.Customer(" ", "123.456.789-12", "11 91234-5678", "fulano@gmail.com"));
        }
    }
}

