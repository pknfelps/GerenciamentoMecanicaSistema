using Dapper;

namespace Repository
{
    internal static class QueryBuilder
    {
        public static QueryData BuildQuery(this string query, Dictionary<string, object?> parameters)
        {
            List<string> queryParams = [];
            DynamicParameters dynamicParameters = new();

            foreach (var parameter in parameters)
            {
                if (string.IsNullOrEmpty(parameter.Value?.ToString()))
                    continue;

                var parameterName = $"Param{queryParams.Count}";
                queryParams.Add($"{parameter.Key} = @{parameterName}");
                dynamicParameters.Add(parameterName, parameter.Value);
            }

            if (queryParams.Count == 0)
                return new QueryData(string.Format(query, ""), dynamicParameters);

            return new QueryData(string.Format(query, string.Join(" AND ", queryParams).Insert(0, "WHERE ")), dynamicParameters);
        }
    }
}
