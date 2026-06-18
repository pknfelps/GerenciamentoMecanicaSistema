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

        private static CreateCustomerDto CustomerToCreate { get; } = new("Fulano", "12345678912", "11912345678", "fulano@gmail.com");
        private static CreateCustomerDto CustomerToCreateFormated { get; set; } = new("Fulano", "123.456.789-12", "(11) 91234-5678", "fulano@gmail.com");
        private static CreateCustomerDto CustomerToFailCreation { get; set; } = new("Teste", "987.654.321-98", "(11) 91234-5678", "teste@gmail.com");

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

        private static readonly Guid ExistingCustomer2Id = Guid.NewGuid();
        private static ICustomer ExistingCustomer2
        {
            get
            {
                var customer = Substitute.For<ICustomer>();
                customer.Id.Returns(ExistingCustomer2Id);
                customer.Name.Returns("Beltrano");
                customer.Document.Id.Returns("12.123.456/0001-15");
                customer.Phone.Number.Returns("(11) 91234-5678");
                customer.Email.Address.Returns("beltrano@gmail.com");
                return customer;
            }
        }

        private static CustomerDto ExistingCustomerDto { get; } = new(ExistingCustomerId, "Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com");
        private static CustomerDto ExistingCustomer2Dto { get; } = new(ExistingCustomer2Id, "Beltrano", "12.123.456/0001-15", "(11) 91234-5678", "beltrano@gmail.com");
        private static CustomerDto CustomerToUpdateDto { get; } = new(ExistingCustomerId, "Ciclano", "12.123.456/0001-12", "(11) 94321-8765", "ciclano.company@gmail.com");
        private static CustomerDto CustomerToFailtUpdateOrDeleteDto { get; } = new(ExistingCustomerId, "Beltrano", "12.123.456/0001-15", "(11) 91234-5678", "beltrano@gmail.com");

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
            CustomerRepository.GetCustomers().Returns(customers);

            CustomerRepository.GetCustomer(Arg.Any<string>()).Returns(callInfo =>
            {
                string document = callInfo.ArgAt<string>(0);

                return customers.FirstOrDefault(x => x.Document.Id.Equals(document));
            });

            CustomerRepository.UpdateCustomer(Arg.Any<ICustomer>()).Returns(callInfo =>
            {
                var costumer = callInfo.ArgAt<ICustomer>(0);

                if (costumer.Document.Id.Equals(ExistingCustomer.Document.Id))
                    return 1;

                return 0;
            });

            CustomerRepository.DeleteCustomer(Arg.Any<string>()).Returns(callInfo =>
            {
                var documento = callInfo.ArgAt<string>(0);

                if (documento.Equals(ExistingCustomer.Document.Id))
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
        public async Task MustGetCustomerByDocumento()
        {
            var cliente = await CustomerService.GetCustomer(ExistingCustomer.Document.Id);

            await CustomerRepository.Received(1).GetCustomer(ExistingCustomer.Document.Id);

            Assert.That(cliente, Is.EqualTo(ExistingCustomerDto));
        }

        [Test]
        public async Task MustGetCustomerByDocumentoWithWrongDocumento()
        {
            var cliente = await CustomerService.GetCustomer(CustomerToCreate.Document);

            await CustomerRepository.Received(1).GetCustomer(CustomerToCreateFormated.Document);

            Assert.That(cliente, Is.Null);
        }

        [Test]
        public async Task MustUpdateCustomer()
        {
            await CustomerService.UpdateCustomer(CustomerToUpdateDto);

            await CustomerRepository.Received(1).UpdateCustomer(Arg.Any<ICustomer>());
        }

        [Test]
        public async Task MustNotUpdateCustomerIfNotExists()
        {
            var customer = new CustomerDto(Guid.NewGuid(), "Teste", "358.410.168-64", "(11) 21245-6458", "teste@gmail.com");

            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.UpdateCustomer(customer));
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToUpdate()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.UpdateCustomer(CustomerToFailtUpdateOrDeleteDto));

            await CustomerRepository.Received(1).UpdateCustomer(Arg.Any<ICustomer>());
        }

        [Test]
        public async Task MustDeleteCustomer()
        {
            await CustomerService.DeleteCustomer(ExistingCustomer.Document.Id);

            await CustomerRepository.Received(1).DeleteCustomer(ExistingCustomer.Document.Id);
        }

        [Test]
        public async Task MustNotDeleteCustomerIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.DeleteCustomer(CustomerToCreate.Document));
        }

        [Test]
        public async Task MustThrowExceptionIfFailToDelete()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.DeleteCustomer(ExistingCustomer2.Document.Id));

            await CustomerRepository.Received(1).DeleteCustomer(ExistingCustomer2.Document.Id);
        }
    }
}
