using Domain.Customer;
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

        private static readonly CustomerDto CustomerToCreate = new(Guid.NewGuid(), "Fulano", "12345678912", "11912345678", "fulano@gmail.com");
        private static readonly CustomerDto CustomerToCreateFormated = new(Guid.NewGuid(), "Fulano", "123.456.789-12", "(11) 91234-5678", "fulano@gmail.com");
        private static readonly CustomerDto CustomerToFailCreation = new(Guid.NewGuid(), "Fulano Fail", "123.123.123-12", "(11) 99999-9999", "fail@gmail.com");

        private static Guid ExistingCustomerId = Guid.NewGuid();

        private static readonly List<ICustomer> ExistingCustomers =
        [
            new Customer(ExistingCustomerId, "Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com"),
            new Customer(Guid.NewGuid(), "Beltrano", "12.123.456/0001-15", "(11) 93214-6578", "beltrano@gmail.com"),
        ];
        private static readonly List<CustomerDto> ExistingCustomersDtos =
        [
            new (ExistingCustomerId, "Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com"),
            new (Guid.NewGuid(), "Beltrano", "12.123.456/0001-15", "(11) 93214-6578", "beltrano@gmail.com"),
        ];
        private static readonly CustomerDto ExistingCustomerDto = new(ExistingCustomerId, "Ciclano", "12.123.456/0001-12", "(11) 91234-5678", "ciclano@gmail.com");
        private static readonly CustomerDto CustomerToUpdateDto = new(ExistingCustomerId, "Ciclano", "12.123.456/0001-12", "(11) 94321-8765", "ciclano.company@gmail.com");
        private static readonly CustomerDto CustomerToFailtUpdateDto = new(ExistingCustomerId, "Ciclano", "12.123.456/0001-15", "(11) 94321-8765", "ciclano.company@gmail.com");

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

            CustomerRepository.GetCustomers().Returns(ExistingCustomers);

            CustomerRepository.GetCustomer(Arg.Any<string>()).Returns(callInfo =>
            {
                string documento = callInfo.ArgAt<string>(0);

                return ExistingCustomers.FirstOrDefault(x => x.Document.Id.Equals(documento));
            });

            CustomerRepository.UpdateCustomer(Arg.Any<ICustomer>()).Returns(callInfo =>
            {
                var cliente = callInfo.ArgAt<ICustomer>(0);

                if (cliente.Document.Id.Equals(ExistingCustomers[0].Document.Id))
                    return 1;

                return 0;
            });

            CustomerRepository.DeleteCustomer(Arg.Any<string>()).Returns(callInfo =>
            {
                var documento = callInfo.ArgAt<string>(0);

                if (documento.Equals(ExistingCustomers[0].Document.Id))
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
                Assert.That(clientes[0], Is.EqualTo(ExistingCustomersDtos[0]));
                Assert.That(clientes[1], Is.EqualTo(ExistingCustomersDtos[1]));
            });
        }

        [Test]
        public async Task MustGetCustomerByDocumento()
        {
            var cliente = await CustomerService.GetCustomer(ExistingCustomers[0].Document.Id);

            await CustomerRepository.Received(1).GetCustomer(ExistingCustomers[0].Document.Id);

            Assert.That(cliente, Is.EqualTo(ExistingCustomersDtos[0]));
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
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.UpdateCustomer(CustomerToCreate));
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToUpdate()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.UpdateCustomer(CustomerToFailtUpdateDto));

            await CustomerRepository.Received(1).UpdateCustomer(Arg.Any<ICustomer>());
        }

        [Test]
        public async Task MustDeleteCustomer()
        {
            await CustomerService.DeleteCustomer(ExistingCustomers[0].Document.Id);

            await CustomerRepository.Received(1).DeleteCustomer(ExistingCustomers[0].Document.Id);
        }

        [Test]
        public async Task MustNotDeleteCustomerIfNotExists()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.DeleteCustomer(CustomerToCreate.Document));
        }

        [Test]
        public async Task MustThrowExceptionIfFailToDelete()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await CustomerService.DeleteCustomer(ExistingCustomers[1].Document.Id));

            await CustomerRepository.Received(1).DeleteCustomer(ExistingCustomers[1].Document.Id);
        }
    }
}
