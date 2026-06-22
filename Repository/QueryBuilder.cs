namespace Repository
{
    internal static class QueryBuilder
    {
        public static string BuildQuery(this string query, Dictionary<string, object?> parameters)
        {
            List<string> queryParams = [];

            foreach (var parameter in parameters)
            {
                if (string.IsNullOrEmpty(parameter.Value?.ToString()))
                    continue;

                queryParams.Add($"{parameter.Key} = '{parameter.Value}'");
            }

            if (queryParams.Count == 0)
                return string.Format(query, "");

            return string.Format(query, string.Join("AND ", queryParams).Insert(0, "WHERE "));
        }
    }
}
