using GerenciamentoMecanicaSistema.Contracts.Responses.Order;
using GerenciamentoMecanicaSistema.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Service.Interface.Observability;

namespace GerenciamentoMecanicaSistema.Observability;

public sealed class OrderBusinessMetricsFilter(
    OrderBusinessMetrics metrics, IOrderMetricsHistoryReader reader, BusinessMetricOptions options,
    ILogger<OrderBusinessMetricsFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        if (result.Exception is not null || result.Canceled
            || context.ActionDescriptor is not ControllerActionDescriptor action
            || action.ControllerTypeInfo.AsType() != typeof(OrdersController)
            || (result.Result as IStatusCodeActionResult)?.StatusCode is not (>= 200 and < 300))
            return;

        try
        {
            if (action.MethodInfo.Name == nameof(OrdersController.CreateOrder)
                && result.Result is ObjectResult { Value: CreateOrderResponse })
            {
                metrics.RecordCreated();
                return;
            }
            var completedStatus = action.MethodInfo.Name switch
            {
                nameof(OrdersController.CompleteDiagnosis) => "WaitingForApproval",
                nameof(OrdersController.CompleteExecution) => "Finished",
                nameof(OrdersController.VehicleDelivered) => "Delivered",
                _ => null
            };
            if (completedStatus is null || !context.ActionArguments.TryGetValue("id", out var value) || value is not Guid id)
                return;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.HistoryQueryTimeoutSeconds));
            var history = await reader.ReadAsync(id, options.HistoryQueryTimeoutSeconds, timeout.Token);
            OrderDurationMeasurements.Record(history, completedStatus, metrics);
        }
        catch (Exception exception)
        {
            metrics.RecordTelemetryFailure();
            logger.LogWarning("Falha ao medir operação persistida da ordem. ExceptionType: {ExceptionType}", exception.GetType().FullName);
        }
    }
}
