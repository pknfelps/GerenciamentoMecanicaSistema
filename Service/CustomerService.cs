using Domain.Customer;
using Domain.Interface.Custumer;
using Service.Interface.Persistence;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.Customer;
using Service.Interface.Results.Customer;
using Service.Interface.CustomerAccess;
using System.Data.Common;

namespace Service
{
    public class CustomerService(ICustomerRepository repository, ICustomerAccessSyncRepository accessSync,
        ITransactionManager transactions, ICustomerAccessSynchronizer synchronizer) : ICustomerService
    {
        private ICustomerRepository Repository { get; set; } = repository;

        public async Task RegisterCustomer(CreateCustomerCommand customer)
        {
            var document = DocumentWrapper.CreateDocument(customer.Document).Id;
            var sync = await transactions.ExecuteInTransaction(async () =>
            {
                await accessSync.LockCustomersAsync();
                var existing = await Repository.GetCustomer(document: document);
                if (existing is not null)
                {
                    var pending = await accessSync.FindAsync(existing.Id);
                    if (pending is { Pending: true, Operation: "create" })
                    {
                        var retry = new Customer(customer.Name, customer.Document, customer.Phone, customer.Email);
                        if (existing.Name == retry.Name && existing.Email.Address == retry.Email.Address
                            && existing.Phone.Number == retry.Phone.Number && existing.Status == retry.Status)
                            return Upsert(existing, pending.Revision);
                    }
                    throw new ConflictException("Cliente já existe no sistema");
                }

                var saved = new Customer(customer.Name, customer.Document, customer.Phone, customer.Email);
                if (await Repository.RegisterCustomer(saved) == 0)
                    throw new ApplicationFailureException("Falha ao criar o cliente");
                return Upsert(saved, await accessSync.MarkPendingAsync(saved.Id, "create"));
            });
            await SynchronizeAsync(sync);
        }

        public async Task<IEnumerable<CustomerResult>> GetCustomers(Guid? id = null, string name = "", string document = "")
        {
            if (!string.IsNullOrEmpty(document))
                document = DocumentWrapper.CreateDocument(document).Id;

            var customers = await Repository.GetCustomers(id, name, document);

            return customers.Select(CreateResult);
        }

        public async Task<CustomerResult?> GetCustomer(Guid? id = null, string name = "", string document = "")
        {
            if (id == null && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(document))
                throw new InvalidRequestException("Falha ao buscar o cliente: nenhum parâmetro foi fornecido");

            if (!string.IsNullOrEmpty(document))
                document = DocumentWrapper.CreateDocument(document).Id;

            var customer = await Repository.GetCustomer(id, name, document);

            if (customer == null)
                return null;

            return CreateResult(customer);
        }

        public async Task UpdateCustomer(Guid id, CreateCustomerCommand customer)
        {
            var sync = await transactions.ExecuteInTransaction<CustomerAccessCommand?>(async () =>
            {
                await accessSync.LockCustomersAsync();
                var existing = await Repository.GetCustomer(id) ?? throw new NotFoundException("Cliente não existe no sistema");
                var saved = new Customer(id, customer.Name, customer.Document, customer.Phone, customer.Email, existing.Status);
                if (await Repository.UpdateCustomer(saved) == 0)
                    throw new ApplicationFailureException("Falha ao atualizar o cliente");

                // The existing PATCH validates the supplied document but preserves the persisted document and status.
                if (existing.Name != saved.Name || existing.Email.Address != saved.Email.Address)
                    return Upsert(saved, await accessSync.MarkPendingAsync(id, "update"), existing.Document.Id);
                var pending = await accessSync.FindAsync(id);
                return pending is { Pending: true } ? Upsert(saved, pending.Revision, existing.Document.Id) : null;
            });
            if (sync is not null)
                await SynchronizeAsync(sync);
        }

        public async Task DeleteCustomer(Guid id)
        {
            var sync = await transactions.ExecuteInTransaction(async () =>
            {
                await accessSync.LockCustomersAsync();
                if (await Repository.GetCustomer(id) is null)
                {
                    if (await accessSync.FindAsync(id) is { Pending: true, Operation: "revoke" } pending)
                        return new CustomerAccessCommand(id, pending.Revision, null);
                    throw new NotFoundException("Cliente não existe no sistema");
                }
                if (await Repository.DeleteCustomer(id) == 0)
                    throw new ApplicationFailureException("Falha ao excluir o cliente");
                return new CustomerAccessCommand(id, await accessSync.MarkPendingAsync(id, "revoke"), null);
            });
            await SynchronizeAsync(sync);
        }

        private static CustomerAccessCommand Upsert(ICustomer customer, long revision, string? persistedDocument = null) =>
            new(customer.Id, revision, new(customer.Name, persistedDocument ?? customer.Document.Id, customer.Email.Address, customer.Status.ToString()));

        private async Task SynchronizeAsync(CustomerAccessCommand sync)
        {
            var outcome = await synchronizer.SynchronizeAsync(sync);
            if (outcome == CustomerAccessSyncOutcome.DocumentConflict)
                throw new ConflictException("Documento já associado a outro registro de acesso");
            if (outcome == CustomerAccessSyncOutcome.Applied)
            {
                try
                {
                    if (await accessSync.ConfirmAsync(sync.CustomerId, sync.Revision))
                        return;
                }
                catch (Exception error) when (error is DbException or TimeoutException or OperationCanceledException)
                {
                    throw new CustomerAccessSyncPendingException(sync.CustomerId);
                }
            }
            throw new CustomerAccessSyncPendingException(sync.CustomerId);
        }

        private static CustomerResult CreateResult(ICustomer customer)
        {
            return new(customer.Id, customer.Name, customer.Document.Id, customer.Phone.Number, customer.Email.Address, customer.Status);
        }
    }
}
