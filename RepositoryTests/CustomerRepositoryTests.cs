using Dapper;
using Domain.Interface.Custumer;
using NSubstitute;
using Repository;
using Repository.Interface;

namespace RepositoryTests
{
    public class CustomerRepositoryTests : BaseRepositoryTests
    {
        private ICustomerRepository Repository { get; set; }

        private static ICustomer CustomerToCreate
        {
            get
            {
                var customer = Substitute.For<ICustomer>();
                customer.Id.Returns(Guid.NewGuid());
                customer.Name.Returns("Fulano");
                customer.Document.Id.Returns("123.456.789-12");
                customer.Phone.Number.Returns("(11) 31234-5678");
                customer.Email.Address.Returns("fulano@gmail.com");
                return customer;
            }
        }

        private static readonly Guid ExistingCustomerId = Guid.NewGuid();
        private static ICustomer ExistingCustomer
        {
            get
            {
                var customer = Substitute.For<ICustomer>();
                customer.Id.Returns(ExistingCustomerId);
                customer.Name.Returns("Ciclano");
                customer.Document.Id.Returns("12.123.456/0001-12");
                customer.Phone.Number.Returns("(11) 91234-5678");
                customer.Email.Address.Returns("ciclano@gmail.com");
                return customer;
            }
        }

        private static ICustomer CustomerToUpdate
        {
            get
            {
                var customer = Substitute.For<ICustomer>();
                customer.Id.Returns(ExistingCustomerId);
                customer.Name.Returns("Ciclano");
                customer.Document.Id.Returns("12.123.456/0001-12");
                customer.Phone.Number.Returns("(11) 94321-8765");
                customer.Email.Address.Returns("ciclano.company@gmail.com");
                return customer;
            }
        }

        protected override async Task InternalSetup()
        {
            await Connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS customers (
                id UUID PRIMARY KEY NOT NULL,
                name TEXT NOT NULL,
                document TEXT NOT NULL,
                phone TEXT NOT NULL,
                email TEXT NOT NULL);
                """);

            Repository = new CustomerRepository(Connection);

            await Repository.RegisterCustomer(ExistingCustomer);
        }

        [Test]
        public async Task MustCreateCustomer()
        {
            var registro = await Repository.RegisterCustomer(CustomerToCreate);

            Assert.That(registro, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustGetAllCustomers()
        {
            var customers = (await Repository.GetCustomers()).ToList();

            Assert.That(customers, Has.Count.EqualTo(1));

            Assert.Multiple(() =>
            {
                Assert.That(customers[0].Name, Is.EqualTo(ExistingCustomer.Name));
                Assert.That(customers[0].Document.Id, Is.EqualTo(ExistingCustomer.Document.Id));
                Assert.That(customers[0].Phone.Number, Is.EqualTo(ExistingCustomer.Phone.Number));
            });
        }

        [Test]
        public async Task MustGetCustomerByDocumento()
        {
            var customer = await Repository.GetCustomer(ExistingCustomer.Document.Id);

            Assert.That(customer, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(customer.Name, Is.EqualTo(ExistingCustomer.Name));
                Assert.That(customer.Document.Id, Is.EqualTo(ExistingCustomer.Document.Id));
                Assert.That(customer.Phone.Number, Is.EqualTo(ExistingCustomer.Phone.Number));
            });
        }

        [Test]
        public async Task MustGetCustomerByDocumentoWithWrongDocumento()
        {
            ICustomer? customer = await Repository.GetCustomer(CustomerToCreate.Document.Id);

            Assert.That(customer, Is.Null);
        }

        [Test]
        public async Task MustUpdateCustomer()
        {
            await Repository.UpdateCustomer(CustomerToUpdate);

            var customer = await Repository.GetCustomer(ExistingCustomer.Document.Id);

            Assert.That(customer, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(customer.Id, Is.EqualTo(ExistingCustomer.Id));
                Assert.That(customer.Name, Is.EqualTo(ExistingCustomer.Name));
                Assert.That(customer.Document.Id, Is.EqualTo(ExistingCustomer.Document.Id));
                Assert.That(customer.Phone.Number, Is.Not.EqualTo(ExistingCustomer.Phone.Number));
                Assert.That(customer.Email.Address, Is.Not.EqualTo(ExistingCustomer.Email.Address));
            });

            Assert.Multiple(() =>
            {
                Assert.That(customer.Id, Is.EqualTo(CustomerToUpdate.Id));
                Assert.That(customer.Name, Is.EqualTo(CustomerToUpdate.Name));
                Assert.That(customer.Document.Id, Is.EqualTo(CustomerToUpdate.Document.Id));
                Assert.That(customer.Phone.Number, Is.EqualTo(CustomerToUpdate.Phone.Number));
                Assert.That(customer.Email.Address, Is.EqualTo(CustomerToUpdate.Email.Address));
            });
        }

        [Test]
        public async Task MustDeleteCustomer()
        {
            await Repository.DeleteCustomer(ExistingCustomer.Document.Id);

            ICustomer? customer = await Repository.GetCustomer(ExistingCustomer.Document.Id);

            Assert.That(customer, Is.Null);
        }
    }
}