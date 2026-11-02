using GerenciamentoMecanicaSistema.Contracts.Requests.Order;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GerenciamentoMecanicaSistema.OpenApi;

// A custom JSON converter owns validation; describe the same strict shape in the generated contract.
public sealed class BudgetRequestSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(ApproveOrderRequest))
        {
            schema.Type = JsonSchemaType.Object;
            schema.Properties = new Dictionary<string, IOpenApiSchema> { ["approved"] = new OpenApiSchema { Type = JsonSchemaType.Boolean } };
            schema.Required = new HashSet<string> { "approved" };
            schema.AdditionalPropertiesAllowed = false;
        }
        return Task.CompletedTask;
    }
}
