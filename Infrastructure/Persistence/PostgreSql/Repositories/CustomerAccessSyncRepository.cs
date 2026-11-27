using Infrastructure.Observability;
using Infrastructure.Persistence.PostgreSql.Transactions;
using Service.Interface.Persistence;
using System.Data;

namespace Infrastructure.Persistence.PostgreSql.Repositories;

public sealed class CustomerAccessSyncRepository(IDbConnection connection, DbTransactionContext transactionContext, IntegrationMetrics? metrics = null)
    : BaseRepository(connection, transactionContext, metrics), ICustomerAccessSyncRepository
{
    public Task LockCustomersAsync() => Database.ExecuteAsync(
        "LOCK TABLE public.customers IN SHARE ROW EXCLUSIVE MODE;", new { },
        Transaction ?? throw new InvalidOperationException("The customer mutation requires a transaction."));

    public Task<CustomerAccessSyncState?> FindAsync(Guid customerId) => Database.QuerySingleOrDefaultAsync<CustomerAccessSyncState>(
        "SELECT revision, operation, pending FROM public.customer_access_sync WHERE customer_id = @CustomerId;",
        new { CustomerId = customerId }, Transaction);

    public Task<long> MarkPendingAsync(Guid customerId, string operation) => Database.QuerySingleOrDefaultAsync<long>("""
        INSERT INTO public.customer_access_sync(customer_id, revision, operation, pending)
        VALUES (@CustomerId, 1, @Operation, true)
        ON CONFLICT (customer_id) DO UPDATE
        SET revision = customer_access_sync.revision + 1, operation = EXCLUDED.operation, pending = true
        RETURNING revision;
        """, new { CustomerId = customerId, Operation = operation },
        Transaction ?? throw new InvalidOperationException("The customer mutation requires a transaction."));

    public async Task<bool> ConfirmAsync(Guid customerId, long revision) =>
        await Database.ExecuteAsync("""
            UPDATE public.customer_access_sync SET pending = false WHERE customer_id = @CustomerId AND revision = @Revision;
            """, new { CustomerId = customerId, Revision = revision }, Transaction) == 1;
}
