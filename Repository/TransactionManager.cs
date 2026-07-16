using Microsoft.Extensions.Logging;
using Repository.Interface;
using System.Data;

namespace Repository
{
    public class TransactionManager(IDbConnection connection, DbTransactionContext transactionContext, ILogger<TransactionManager> logger) : ITransactionManager
    {
        private IDbConnection Connection { get; } = connection;
        private DbTransactionContext TransactionContext { get; } = transactionContext;
        private ILogger<TransactionManager> Logger { get; } = logger;

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
            catch (Exception e)
            {
                transaction.Rollback();
                Logger.LogError(e, "Transação revertida após falha durante execução de operação persistente");
                throw;
            }
            finally
            {
                TransactionContext.Current = null;
            }
        }
    }
}
