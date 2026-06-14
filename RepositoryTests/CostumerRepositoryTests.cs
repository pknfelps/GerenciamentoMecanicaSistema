using Dapper;
using Domain.Customer;
using Domain.Interface.Custumer;
using Repository;
using Repository.Interface;

namespace RepositoryTests
{
    public class CostumerRepositoryTests : BaseRepositoryTests
    {
        private ICustomerRepository Repository { get; set; }

        private static readonly ICustomer CostumerToCreate = new Customer("Fulano", "123.456.789-12", "(11) 31234-5678", "fulano@gmail.com");
        private static readonly Guid ExistingCostumerId = Guid.NewGuid();
        private static readonly List<ICustomer> ExistingCostumers =
        [
            new Customer(ExistingCostumerId, "Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com"),
            new Customer("Beltrano", "12.123.456/0001-15", "(11) 93214-6578", "beltrano@gmail.com"),
        ];
        private static readonly ICustomer CostumerToUpdate = new Customer(ExistingCostumerId, "Ciclano", "12.123.456/0001-12", "(11) 94321-8765", "ciclano.company@gmail.com");

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

            foreach (ICustomer Costumer in ExistingCostumers)
                await Repository.RegisterCustomer(Costumer);
        }

        [Test]
        public async Task MustCreateCostumer()
        {
            var registro = await Repository.RegisterCustomer(CostumerToCreate);

            Assert.That(registro, Is.Not.EqualTo(0));
        }

        [Test]
        public async Task MustGetAllCostumers()
        {
            var Costumers = (await Repository.GetCustomers()).ToList();

            Assert.That(Costumers, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(Costumers[0].Name, Is.EqualTo(ExistingCostumers[0].Name));
                Assert.That(Costumers[0].Document.Id, Is.EqualTo(ExistingCostumers[0].Document.Id));
                Assert.That(Costumers[0].Phone.Number, Is.EqualTo(ExistingCostumers[0].Phone.Number));
            });

            Assert.Multiple(() =>
            {
                Assert.That(Costumers[1].Name, Is.EqualTo(ExistingCostumers[1].Name));
                Assert.That(Costumers[1].Document.Id, Is.EqualTo(ExistingCostumers[1].Document.Id));
                Assert.That(Costumers[1].Phone.Number, Is.EqualTo(ExistingCostumers[1].Phone.Number));
            });
        }

        [Test]
        public async Task MustGetCostumerByDocumento()
        {
            var Costumer = await Repository.GetCustomer(ExistingCostumers[0].Document.Id);

            Assert.That(Costumer, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(Costumer.Name, Is.EqualTo(ExistingCostumers[0].Name));
                Assert.That(Costumer.Document.Id, Is.EqualTo(ExistingCostumers[0].Document.Id));
                Assert.That(Costumer.Phone.Number, Is.EqualTo(ExistingCostumers[0].Phone.Number));
            });
        }

        [Test]
        public async Task MustGetCostumerByDocumentoWithWrongDocumento()
        {
            ICustomer? Costumer = await Repository.GetCustomer(CostumerToCreate.Document.Id);

            Assert.That(Costumer, Is.Null);
        }

        [Test]
        public async Task MustUpdateCostumer()
        {
            await Repository.UpdateCustomer(CostumerToUpdate);

            var Costumer = await Repository.GetCustomer(ExistingCostumers[0].Document.Id);

            Assert.That(Costumer, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(Costumer.Id, Is.EqualTo(ExistingCostumers[0].Id));
                Assert.That(Costumer.Name, Is.EqualTo(ExistingCostumers[0].Name));
                Assert.That(Costumer.Document.Id, Is.EqualTo(ExistingCostumers[0].Document.Id));
                Assert.That(Costumer.Phone.Number, Is.Not.EqualTo(ExistingCostumers[0].Phone.Number));
                Assert.That(Costumer.Email.Address, Is.Not.EqualTo(ExistingCostumers[0].Email.Address));
            });

            Assert.Multiple(() =>
            {
                Assert.That(Costumer.Id, Is.EqualTo(CostumerToUpdate.Id));
                Assert.That(Costumer.Name, Is.EqualTo(CostumerToUpdate.Name));
                Assert.That(Costumer.Document.Id, Is.EqualTo(CostumerToUpdate.Document.Id));
                Assert.That(Costumer.Phone.Number, Is.EqualTo(CostumerToUpdate.Phone.Number));
                Assert.That(Costumer.Email.Address, Is.EqualTo(CostumerToUpdate.Email.Address));
            });
        }

        [Test]
        public async Task MustDeleteCostumer()
        {
            await Repository.DeleteCustomer(ExistingCostumers[0].Document.Id);

            ICustomer? Costumer = await Repository.GetCustomer(ExistingCostumers[0].Document.Id);

            Assert.That(Costumer, Is.Null);
        }
    }
}