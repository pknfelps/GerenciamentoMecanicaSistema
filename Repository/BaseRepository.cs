using System.Data;

namespace Repository
{
    public abstract class BaseRepository(IDbConnection connection)
    {
        protected IDbConnection Connection { get; private set; } = connection;
    }
}
