using Microsoft.Extensions.Logging;
using Service.Interface.Persistence;
using System.Data;
using Infrastructure.Observability;
using Infrastructure.Persistence.PostgreSql.Observability;

namespace Infrastructure.Persistence.PostgreSql.Transactions
{
    public class TransactionManager(IDbConnection connection, DbTransactionContext transactionContext, ILogger<TransactionManager> logger, IntegrationMetrics? metrics = null) : ITransactionManager
    {
        private PostgreSqlOperations Database { get; } = new(connection, metrics);
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

            using var transaction = Database.BeginTransaction();
            TransactionContext.Current = transaction;

            try
            {
                var result = await operation();
                Database.Commit(transaction);

                return result;
            }
            catch (Exception e)
            {
                Database.Rollback(transaction);
                Logger.LogError(
                    "Transação revertida após falha durante execução de operação persistente. ExceptionType: {ExceptionType}",
                    e.GetType().FullName);
                throw;
            }
            finally
            {
                TransactionContext.Current = null;
            }
        }
    }
}
