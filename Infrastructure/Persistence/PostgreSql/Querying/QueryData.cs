using Dapper;

namespace Infrastructure.Persistence.PostgreSql.Querying
{
    internal sealed class QueryData(string sql, DynamicParameters parameters)
    {
        public string Sql { get; } = sql;
        public DynamicParameters Parameters { get; } = parameters;
    }
}
