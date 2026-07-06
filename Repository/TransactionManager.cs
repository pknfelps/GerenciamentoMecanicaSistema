using Repository.Interface;
using System.Data;

namespace Repository
{
    public class TransactionManager(IDbConnection connection, DbTransactionContext transactionContext) : ITransactionManager
    {
        private IDbConnection Connection { get; } = connection;
        private DbTransactionContext TransactionContext { get; } = transactionContext;

        public async Task ExecuteInTransaction(Func<Task> operation)
        {
            await ExecuteInTransaction(async () =>
            {
                await operation();
                return true;
            });
        }

        public async Task<T> ExecuteInTransaction<T>(Func<Task<T>> operation)
        {
            if (TransactionContext.Current != null)
                return await operation();

            using var transaction = Connection.BeginTransaction();
            TransactionContext.Current = transaction;

            try
            {
                var result = await operation();
                transaction.Commit();

                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                TransactionContext.Current = null;
            }
        }
    }
}
