using Domain.Interface.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Service.Interface.Exceptions;
using System.Diagnostics;
using GerenciamentoMecanicaSistema.Controllers;
using GerenciamentoMecanicaSistema.Observability;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace GerenciamentoMecanicaSistema.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger,
        OrderBusinessMetrics? metrics = null)
    {
        private RequestDelegate Next { get; } = next;
        private ILogger<ExceptionHandlingMiddleware> Logger { get; } = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            var method = context.Request.Method is "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS"
                ? context.Request.Method
                : "OTHER";
            var requestId = context.TraceIdentifier;
            var traceId = Activity.Current?.TraceId.ToString() ?? "unavailable";

            try
            {
                await Next(context);
            }
            catch (ApplicationBaseException exception)
            {
                Logger.LogWarning(
                    "Erro de aplicação tratado. StatusCode: {StatusCode}. ExceptionType: {ExceptionType}. RequestId: {RequestId}. TraceId: {TraceId}. HttpMethod: {HttpMethod}. Route: {Route}",
                    GetStatusCode(exception),
                    exception.GetType().FullName, requestId, traceId, method, route);

                await WriteProblemDetails(context, exception, GetStatusCode(exception));
            }
            catch (DomainBaseException exception)
            {
                Logger.LogWarning(
                    "Erro de domínio tratado. StatusCode: {StatusCode}. ExceptionType: {ExceptionType}. RequestId: {RequestId}. TraceId: {TraceId}. HttpMethod: {HttpMethod}. Route: {Route}",
                    GetStatusCode(exception),
                    exception.GetType().FullName, requestId, traceId, method, route);

                await WriteProblemDetails(context, exception, GetStatusCode(exception));
            }
            catch (Exception exception)
            {
                Logger.LogError(
                    "Erro inesperado durante a requisição. StatusCode: {StatusCode}. ExceptionType: {ExceptionType}. RequestId: {RequestId}. TraceId: {TraceId}. HttpMethod: {HttpMethod}. Route: {Route}",
                    StatusCodes.Status500InternalServerError,
                    exception.GetType().FullName, requestId, traceId, method, route);

                await WriteProblemDetails(
                    context,
                    exception,
                    StatusCodes.Status500InternalServerError,
                    "Erro interno do servidor",
                    "Ocorreu um erro inesperado ao processar a requisição.");
            }
            finally
            {
                if (context.Response.StatusCode == StatusCodes.Status500InternalServerError && !context.RequestAborted.IsCancellationRequested
                    && context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is { } action
                    && action.ControllerTypeInfo.AsType() == typeof(OrdersController))
                    metrics?.RecordProcessingFailure(action.MethodInfo.Name);
            }
        }

        private static int GetStatusCode(ApplicationBaseException exception) =>
            exception switch
            {
                InvalidRequestException => StatusCodes.Status400BadRequest,
                AccessDeniedException => StatusCodes.Status403Forbidden,
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
                ApplicationFailureException => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            };

        private static int GetStatusCode(DomainBaseException exception) =>
            exception switch
            {
                DomainValidationException => StatusCodes.Status400BadRequest,
                DomainBusinessRuleException => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status500InternalServerError
            };

        private static async Task WriteProblemDetails(HttpContext context, Exception exception, int statusCode, string? title = null, string? detail = null)
        {
            if (context.Response.HasStarted)
                throw exception;

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title ?? GetTitle(statusCode),
                Detail = detail ?? exception.Message,
                Instance = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
        }

        private static string GetTitle(int statusCode) =>
            statusCode switch
            {
                StatusCodes.Status400BadRequest => "Requisição inválida",
                StatusCodes.Status403Forbidden => "Acesso negado",
                StatusCodes.Status404NotFound => "Recurso não encontrado",
                StatusCodes.Status409Conflict => "Conflito",
                StatusCodes.Status422UnprocessableEntity => "Regra de negócio violada",
                _ => "Erro interno do servidor"
            };
    }
}
