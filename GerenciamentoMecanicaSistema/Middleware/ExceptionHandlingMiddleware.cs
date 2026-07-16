using Domain.Interface.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Service.Interface.Exceptions;

namespace GerenciamentoMecanicaSistema.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        private RequestDelegate Next { get; } = next;
        private ILogger<ExceptionHandlingMiddleware> Logger { get; } = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await Next(context);
            }
            catch (ApplicationBaseException exception)
            {
                Logger.LogWarning(
                    exception,
                    "Erro de aplicação tratado. Path: {Path}. StatusCode: {StatusCode}",
                    context.Request.Path,
                    GetStatusCode(exception));

                await WriteProblemDetails(context, exception, GetStatusCode(exception));
            }
            catch (DomainBaseException exception)
            {
                Logger.LogWarning(
                    exception,
                    "Erro de domínio tratado. Path: {Path}. StatusCode: {StatusCode}",
                    context.Request.Path,
                    GetStatusCode(exception));

                await WriteProblemDetails(context, exception, GetStatusCode(exception));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Erro inesperado durante a requisição. Path: {Path}", context.Request.Path);

                await WriteProblemDetails(
                    context,
                    exception,
                    StatusCodes.Status500InternalServerError,
                    "Erro interno do servidor",
                    "Ocorreu um erro inesperado ao processar a requisição.");
            }
        }

        private static int GetStatusCode(ApplicationBaseException exception) =>
            exception switch
            {
                InvalidRequestException => StatusCodes.Status400BadRequest,
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

            await context.Response.WriteAsJsonAsync(problemDetails);
        }

        private static string GetTitle(int statusCode) =>
            statusCode switch
            {
                StatusCodes.Status400BadRequest => "Requisição inválida",
                StatusCodes.Status404NotFound => "Recurso não encontrado",
                StatusCodes.Status409Conflict => "Conflito",
                StatusCodes.Status422UnprocessableEntity => "Regra de negócio violada",
                _ => "Erro interno do servidor"
            };
    }
}
