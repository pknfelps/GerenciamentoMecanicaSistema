using System.Data;

namespace Repository
{
    public class DbTransactionContext
    {
        public IDbTransaction? Current { get; set; }
    }
}
