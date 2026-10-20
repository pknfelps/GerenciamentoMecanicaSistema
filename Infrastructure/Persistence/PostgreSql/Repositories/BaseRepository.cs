using System.Data;
using Infrastructure.Observability;
using Infrastructure.Persistence.PostgreSql.Observability;

using Infrastructure.Persistence.PostgreSql.Transactions;

namespace Infrastructure.Persistence.PostgreSql.Repositories
{
    public abstract class BaseRepository(IDbConnection connection, DbTransactionContext? transactionContext = null, IntegrationMetrics? metrics = null)
    {
        protected PostgreSqlOperations Database { get; } = new(connection, metrics);
        protected IDbTransaction? Transaction => transactionContext?.Current;
    }
}
