using System.Data;

namespace Infrastructure.Persistence.PostgreSql.Transactions
{
    public class DbTransactionContext
    {
        public IDbTransaction? Current { get; set; }
    }
}
