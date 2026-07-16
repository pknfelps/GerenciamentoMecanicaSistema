using Dapper;

namespace Repository
{
    internal sealed class QueryData(string sql, DynamicParameters parameters)
    {
        public string Sql { get; } = sql;
        public DynamicParameters Parameters { get; } = parameters;
    }
}
