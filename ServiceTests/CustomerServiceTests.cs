using Domain.Interface.Custumer;
using NSubstitute;
using NSubstitute.Extensions;
using Service.Interface.Persistence;
using Service;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Customer;
using Service.Interface.Results.Customer;
using Service.Interface.CustomerAccess;

namespace ServiceTests
{
    public class CustomerServiceTests
    {
        private ICustomerService CustomerService { get; set; }
        private ICustomerRepository CustomerRepository { get; set; }
        private ICustomerAccessSyncRepository AccessSync { get; set; }
        private ICustomerAccessSynchronizer Identity { get; set; }

        private static CreateCustomerCommand CustomerToCreate { get; } = new("Fulano", "66211973063", "11912345678", "fulano@gmail.com");
        private static CreateCustomerCommand CustomerToCreateFormated { get; set; } = new("Fulano", "662.119.730-63", "(11) 91234-5678", "fulano@gmail.com");
        private static CreateCustomerCommand CustomerToFailCreation { get; set; } = new("Teste", "274.465.520-18", "(11) 91234-5678", "teste@gmail.com");

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

        private static CustomerResult ExistingCustomerResult { get; } = new(ExistingCustomerId, "Ciclano", "10.359.666/0001-94", "(11) 91234-5678", "ciclano@gmail.com", CustomerStatus.Active);
        private static CustomerResult ExistingCustomer2Result { get; } = new(ExistingCustomer2Id, "Beltrano", "65.457.513/0001-71", "(11) 91234-5678", "beltrano@gmail.com", CustomerStatus.Active);
        private static CreateCustomerCommand ExistingCustomerCommand { get; } = new("Ciclano", "10.359.666/0001-94", "(11) 91234-5678", "ciclano@gmail.com");
        private static CreateCustomerCommand CustomerToUpdate { get; } = new("Ciclano", "10.359.666/0001-94", "(11) 94321-8765", "ciclano.company@gmail.com");
        private static CreateCustomerCommand CustomerToFailUpdateOrDelete { get; } = new("Beltrano", "65.457.513/0001-71", "(11) 91234-5678", "beltrano@gmail.com");

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

            AccessSync = Substitute.For<ICustomerAccessSyncRepository>();
            AccessSync.MarkPendingAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(1L);
            AccessSync.ConfirmAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(true);
            Identity = Substitute.For<ICustomerAccessSynchronizer>();
            var inTransaction = false;
            Identity.SynchronizeAsync(Arg.Any<CustomerAccessCommand>()).Returns(_ =>
            {
                Assert.That(inTransaction, Is.False);
                return CustomerAccessSyncOutcome.Applied;
            });
            var transactions = Substitute.For<ITransactionManager>();
            transactions.ExecuteInTransaction(Arg.Any<Func<Task<CustomerAccessCommand?>>>()).Returns(async call =>
            {
                inTransaction = true;
                try { return await call.Arg<Func<Task<CustomerAccessCommand?>>>()(); }
                finally { inTransaction = false; }
            });
            CustomerService = new CustomerService(CustomerRepository, AccessSync, transactions, Identity);
        }

        [Test]
        public async Task MustCreateCustomer()
        {
            await CustomerService.RegisterCustomer(CustomerToCreate);

            await CustomerRepository.ReceivedWithAnyArgs(1).RegisterCustomer(Arg.Any<ICustomer>());
            await CustomerRepository.Received(1).RegisterCustomer(Arg.Is<ICustomer>(customer => customer.Status == CustomerStatus.Active));
        }

        [Test]
        public async Task MustNotCreateCustomerIfExists()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await CustomerService.RegisterCustomer(ExistingCustomerCommand));
        }

        [Test]
        public async Task RepeatsOnlyTheSamePendingRegistrationUsingItsOriginalIdAndRevision()
        {
            ICustomer? saved = null;
            CustomerRepository.Configure().RegisterCustomer(Arg.Any<ICustomer>()).Returns(call =>
            {
                saved = call.Arg<ICustomer>();
                return 1;
            });
            CustomerRepository.Configure().GetCustomer(document: CustomerToCreateFormated.Document).Returns(_ => saved);
            AccessSync.FindAsync(Arg.Any<Guid>()).Returns(new CustomerAccessSyncState(1, "create", true));
            Identity.Configure().SynchronizeAsync(Arg.Any<CustomerAccessCommand>()).Returns(
                CustomerAccessSyncOutcome.Pending, CustomerAccessSyncOutcome.Applied);

            var failure = Assert.ThrowsAsync<CustomerAccessSyncPendingException>(() => CustomerService.RegisterCustomer(CustomerToCreate));
            Assert.That(failure!.CustomerId, Is.EqualTo(saved!.Id));
            var different = new CreateCustomerCommand("Outro Cliente", CustomerToCreate.Document, CustomerToCreate.Phone, CustomerToCreate.Email);
            Assert.ThrowsAsync<ConflictException>(() => CustomerService.RegisterCustomer(different));
            await CustomerService.RegisterCustomer(CustomerToCreateFormated);

            await CustomerRepository.ReceivedWithAnyArgs(1).RegisterCustomer(default!);
            await AccessSync.Received(1).MarkPendingAsync(saved.Id, "create");
            await Identity.Received(2).SynchronizeAsync(Arg.Is<CustomerAccessCommand>(x => x.CustomerId == saved.Id
                && x.Revision == 1 && x.Access!.Document == CustomerToCreateFormated.Document));
        }

        [Test]
        public async Task RetriesPendingRevocationAfterOperationalDeletion()
        {
            CustomerRepository.Configure().GetCustomer(id: ExistingCustomerId).Returns(_ => ExistingCustomer, _ => null);
            AccessSync.FindAsync(ExistingCustomerId).Returns(new CustomerAccessSyncState(1, "revoke", true));
            Identity.Configure().SynchronizeAsync(Arg.Any<CustomerAccessCommand>()).Returns(
                CustomerAccessSyncOutcome.Pending, CustomerAccessSyncOutcome.Applied);

            Assert.ThrowsAsync<CustomerAccessSyncPendingException>(() => CustomerService.DeleteCustomer(ExistingCustomerId));
            await CustomerService.DeleteCustomer(ExistingCustomerId);

            await CustomerRepository.Received(1).DeleteCustomer(ExistingCustomerId);
            await Identity.Received(2).SynchronizeAsync(Arg.Is<CustomerAccessCommand>(x => x.CustomerId == ExistingCustomerId
                && x.Revision == 1 && x.Action == "revoke" && x.Access == null));
        }

        [Test]
        public async Task PhoneOnlyUpdateDoesNotInvokeIdentityWhenAlreadySynchronized()
        {
            var existing = ExistingCustomer;
            var input = new CreateCustomerCommand(existing.Name, existing.Document.Id, "11943218765", existing.Email.Address);
            AccessSync.FindAsync(existing.Id).Returns(new CustomerAccessSyncState(2, "update", false));

            await CustomerService.UpdateCustomer(existing.Id, input);

            await Identity.DidNotReceiveWithAnyArgs().SynchronizeAsync(default!);
            await AccessSync.DidNotReceiveWithAnyArgs().MarkPendingAsync(default, default!);
        }

        [Test]
        public async Task OlderAcknowledgementDoesNotConfirmTheCurrentMutation()
        {
            AccessSync.ConfirmAsync(ExistingCustomerId, 1).Returns(false);
            Assert.ThrowsAsync<CustomerAccessSyncPendingException>(() => CustomerService.UpdateCustomer(ExistingCustomerId, CustomerToUpdate));
            await Identity.Received(1).SynchronizeAsync(Arg.Is<CustomerAccessCommand>(x => x.CustomerId == ExistingCustomerId
                && x.Access!.Email == CustomerToUpdate.Email));
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToCreateCustomer()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await CustomerService.RegisterCustomer(CustomerToFailCreation));

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
                Assert.That(clientes[0], Is.EqualTo(ExistingCustomerResult));
                Assert.That(clientes[1], Is.EqualTo(ExistingCustomer2Result));
            });
        }

        [Test]
        public async Task MustGetAllCustomersWithSameDocument()
        {
            var clientes = (await CustomerService.GetCustomers(document: ExistingCustomer.Document.Id)).ToList();

            await CustomerRepository.Received(1).GetCustomers(document: ExistingCustomer.Document.Id);

            Assert.That(clientes, Has.Count.EqualTo(1));
            Assert.That(clientes[0], Is.EqualTo(ExistingCustomerResult));
        }

        [Test]
        public async Task MustGetCustomerByDocument()
        {
            var cliente = await CustomerService.GetCustomer(document: ExistingCustomer.Document.Id);

            await CustomerRepository.Received(1).GetCustomer(document: ExistingCustomer.Document.Id);

            Assert.That(cliente, Is.EqualTo(ExistingCustomerResult));
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
            Assert.CatchAsync<ApplicationBaseException>(async () => await CustomerService.GetCustomer());

            await CustomerRepository.Received(0).GetCustomer();
        }

        [Test]
        public async Task MustUpdateCustomer()
        {
            await CustomerService.UpdateCustomer(ExistingCustomer.Id, CustomerToUpdate);

            await CustomerRepository.Received(1).UpdateCustomer(Arg.Any<ICustomer>());
        }

        [Test]
        public async Task MustNotUpdateCustomerIfNotExists()
        {
            var customer = new CreateCustomerCommand("Teste", "358.410.168-64", "(11) 21245-6458", "teste@gmail.com");

            Assert.CatchAsync<ApplicationBaseException>(async () => await CustomerService.UpdateCustomer(Guid.NewGuid(), customer));
        }

        [Test]
        public async Task MustPreserveInactiveStatusWhenUpdatingCustomer()
        {
            var existing = ExistingCustomer;
            existing.Status.Returns(CustomerStatus.Inactive);
            CustomerRepository.Configure().GetCustomer(id: ExistingCustomerId).Returns(existing);

            await CustomerService.UpdateCustomer(ExistingCustomerId, CustomerToUpdate);

            await CustomerRepository.Received(1).UpdateCustomer(Arg.Is<ICustomer>(customer =>
                customer.Id == ExistingCustomerId && customer.Status == CustomerStatus.Inactive));
        }

        [Test]
        public async Task MustThrowExceptionIfFailedToUpdate()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await CustomerService.UpdateCustomer(ExistingCustomerId, CustomerToFailUpdateOrDelete));

            await CustomerRepository.Received(1).UpdateCustomer(Arg.Any<ICustomer>());
        }

        [Test]
        public async Task MustDeleteCustomer()
        {
            await CustomerService.DeleteCustomer(ExistingCustomer.Id);

            await CustomerRepository.Received(1).GetCustomer(ExistingCustomer.Id);
            await CustomerRepository.Received(1).DeleteCustomer(ExistingCustomer.Id);
        }

        [Test]
        public async Task MustNotDeleteCustomerIfNotExists()
        {
            Assert.CatchAsync<NotFoundException>(async () => await CustomerService.DeleteCustomer(Guid.NewGuid()));

            await CustomerRepository.ReceivedWithAnyArgs(0).DeleteCustomer(Arg.Any<Guid>());
        }

        [Test]
        public async Task MustThrowExceptionIfFailToDelete()
        {
            Assert.CatchAsync<ApplicationBaseException>(async () => await CustomerService.DeleteCustomer(ExistingCustomer2.Id));

            await CustomerRepository.Received(1).DeleteCustomer(ExistingCustomer2.Id);
        }
    }
}

