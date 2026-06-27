using Domain.Interface.Custumer;
using NSubstitute;
using Repository.Interface;
using Service;
using Service.Interface;
using Service.Interface.Dto.Customer;

namespace ServiceTests
{
    public class CustomerServiceTests
    {
        private ICustomerService CustomerService { get; set; }
        private ICustomerRepository CustomerRepository { get; set; }

        private static CreateCustomerDto CustomerToCreate { get; } = new("Fulano", "66211973063", "11912345678", "fulano@gmail.com");
        private static CreateCustomerDto CustomerToCreateFormated { get; set; } = new("Fulano", "662.119.730-63", "(11) 91234-5678", "fulano@gmail.com");
        private static CreateCustomerDto CustomerToFailCreation { get; set; } = new("Teste", "274.465.520-18", "(11) 91234-5678", "teste@gmail.com");

        private static readonly Guid ExistingCustomerId = Guid.NewGuid();
        private static ICustomer ExistingCustomer
        {
            get
            {
                var customer = Substitute.For<ICustomer>();
                customer.Id.Returns(ExistingCustomerId);
                customer.Name.Returns("Ciclano");
                customer.Document.Id.Returns("10.359.666/0001-94");
                customer.Phone.Number.Returns("(11) 91234-5678");
                customer.Email.Address.Returns("ciclano@gmail.com");
                return customer;
            }
        }

        private static readonly Guid ExistingCustomer2Id = Guid.NewGuid();
        private static ICustomer ExistingCustomer2
        {
            get
            {
                var customer = Substitute.For<ICustomer>();
                customer.Id.Returns(ExistingCustomer2Id);
                customer.Name.Returns("Beltrano");
                customer.Document.Id.Returns("65.457.513/0001-71");
                customer.Phone.Number.Returns("(11) 91234-5678");
                customer.Email.Address.Returns("beltrano@gmail.com");
                return customer;
            }
        }

        private static CustomerDto ExistingCustomerDto { get; } = new(ExistingCustomerId, "Ciclano", "10.359.666/0001-94", "(11) 91234-5678", "ciclano@gmail.com");
        private static CustomerDto ExistingCustomer2Dto { get; } = new(ExistingCustomer2Id, "Beltrano", "65.457.513/0001-71", "(11) 91234-5678", "beltrano@gmail.com");
        private static CreateCustomerDto CustomerToUpdateDto { get; } = new("Ciclano", "10.359.666/0001-94", "(11) 94321-8765", "ciclano.company@gmail.com");
        private static CreateCustomerDto CustomerToFailtUpdateOrDeleteDto { get; } = new("Beltrano", "65.457.513/0001-71", "(11) 91234-5678", "beltrano@gmail.com");

        [SetUp]
        public void SetUp()
        {
            CustomerRepository = Substitute.For<ICustomerRepository>();

            CustomerRepository.RegisterCustomer(Arg.Any<ICustomer>()).Returns(callInfo =>
            {
                var cliente = callInfo.ArgAt<ICustomer>(0);

                if (cliente.Document.Id.Equals(CustomerToCreateFormated.Document))
                    return 1;

                return 0;
            });

            List<ICustomer> customers = new List<ICustomer>() { ExistingCustomer, ExistingCustomer2 };

            CustomerRepository.GetCustomers(document: Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(2);

                if (!string.IsNullOrEmpty(document))
                    return [ExistingCustomer];

                return customers;
            });

            CustomerRepository.GetCustomer(id: Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                return customers.FirstOrDefault(x => x.Id == id);
            });

            CustomerRepository.GetCustomer(name: Arg.Any<string>()).Returns(callInfo =>
            {
                var name = callInfo.ArgAt<string>(1);

                return customers.FirstOrDefault(x => x.Name.Equals(name));
            });

            CustomerRepository.GetCustomer(document: Arg.Any<string>()).Returns(callInfo =>
            {
                var document = callInfo.ArgAt<string>(2);

                return customers.FirstOrDefault(x => x.Document.Id.Equals(document));
            });

            CustomerRepository.UpdateCustomer(Arg.Any<ICustomer>()).Returns(callInfo =>
            {
                var costumer = callInfo.ArgAt<ICustomer>(0);

                if (costumer.Document.Id.Equals(ExistingCustomer.Document.Id))
                    return 1;

                return 0;
            });

            CustomerRepository.DeleteCustomer(Arg.Any<Guid>()).Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);

                if (id.Equals(ExistingCustomer.Id))
                    return 1;

                return 0;
            });

            CustomerService = new CustomerService(CustomerRepository);
        }

        [Test]
        public async Task MustCreateCustomer()
        {
            await CustomerService.RegisterCustomer(CustomerToCreate);

            await CustomerRepository.ReceivedWithAnyArgs(1).RegisterCustomer(Arg.Any<ICustomer>());
        }

        [Test]
        public async Task MustNotCreateCustomerIfExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.RegisterCustomer(ExistingCustomerDto));
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToCreateCustomer()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.RegisterCustomer(CustomerToFailCreation));

            await CustomerRepository.ReceivedWithAnyArgs(1).RegisterCustomer(Arg.Any<ICustomer>());
        }

        [Test]
        public async Task MustGetAllCustomers()
        {
            var clientes = (await CustomerService.GetCustomers()).ToList();

            await CustomerRepository.Received(1).GetCustomers();

            Assert.That(clientes, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(clientes[0].Equals(ExistingCustomerDto), Is.True);
                Assert.That(clientes[1].Equals(ExistingCustomer2Dto), Is.True);
            });
        }

        [Test]
        public async Task MustGetAllCustomersWithSameDocument()
        {
            var clientes = (await CustomerService.GetCustomers(document: ExistingCustomer.Document.Id)).ToList();

            await CustomerRepository.Received(1).GetCustomers(document: ExistingCustomer.Document.Id);

            Assert.That(clientes, Has.Count.EqualTo(1));
            Assert.That(clientes[0].Equals(ExistingCustomerDto), Is.True);
        }

        [Test]
        public async Task MustGetCustomerByDocument()
        {
            var cliente = await CustomerService.GetCustomer(document: ExistingCustomer.Document.Id);

            await CustomerRepository.Received(1).GetCustomer(document: ExistingCustomer.Document.Id);

            Assert.That(cliente, Is.EqualTo(ExistingCustomerDto));
        }

        [Test]
        public async Task MustNotGetCustomerByDocumentWithWrongDocument()
        {
            var cliente = await CustomerService.GetCustomer(document: CustomerToCreate.Document);

            await CustomerRepository.Received(1).GetCustomer(document: CustomerToCreateFormated.Document);

            Assert.That(cliente, Is.Null);
        }

        [Test]
        public async Task MustNotGetCustomerWithNoParameters()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.GetCustomer());

            await CustomerRepository.Received(0).GetCustomer();
        }

        [Test]
        public async Task MustUpdateCustomer()
        {
            await CustomerService.UpdateCustomer(ExistingCustomer.Id, CustomerToUpdateDto);

            await CustomerRepository.Received(1).UpdateCustomer(Arg.Any<ICustomer>());
        }

        [Test]
        public async Task MustNotUpdateCustomerIfNotExists()
        {
            var customer = new CreateCustomerDto("Teste", "358.410.168-64", "(11) 21245-6458", "teste@gmail.com");

            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.UpdateCustomer(Guid.NewGuid(), customer));
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToUpdate()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.UpdateCustomer(ExistingCustomerId, CustomerToFailtUpdateOrDeleteDto));

            await CustomerRepository.Received(1).UpdateCustomer(Arg.Any<ICustomer>());
        }

        [Test]
        public async Task MustDeleteCustomer()
        {
            await CustomerService.DeleteCustomer(ExistingCustomer.Id);

            await CustomerRepository.Received(1).DeleteCustomer(ExistingCustomer.Id);
        }

        [Test]
        public async Task MustNotDeleteCustomerIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.DeleteCustomer(Guid.NewGuid()));
        }

        [Test]
        public async Task MustThrowExceptionIfFailToDelete()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.DeleteCustomer(ExistingCustomer2.Id));

            await CustomerRepository.Received(1).DeleteCustomer(ExistingCustomer2.Id);
        }
    }
}
