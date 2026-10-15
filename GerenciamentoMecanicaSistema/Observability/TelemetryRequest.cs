using Microsoft.AspNetCore.Routing;

namespace GerenciamentoMecanicaSistema.Observability;

public static class TelemetryRequest
{
    public static bool IsProbe(HttpContext? context, TelemetryCollectionOptions collection)
    {
        var path = context?.Request.Path.Value?.TrimEnd('/');
        return collection.ExcludedPaths.Any(excluded => string.Equals(path, excluded.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
    }

    public static string Method(string? method, TelemetryCollectionOptions collection) =>
        method is not null && collection.HttpMethods.Contains(method) ? method : "OTHER";

    public static string Route(HttpContext context) =>
        (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
}
