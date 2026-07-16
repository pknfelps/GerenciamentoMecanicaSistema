using System.Data;

namespace Repository
{
    public abstract class BaseRepository(IDbConnection connection, DbTransactionContext? transactionContext = null)
    {
        protected IDbConnection Connection { get; private set; } = connection;
        protected IDbTransaction? Transaction => transactionContext?.Current;
    }
}
