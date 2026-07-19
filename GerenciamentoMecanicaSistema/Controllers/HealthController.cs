using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("health")]
    [Produces("text/plain")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class HealthController(HealthCheckService healthCheckService) : ControllerBase
    {
        private HealthCheckService HealthCheckService { get; set; } = healthCheckService;
        private const string LiveTag = "live";
        private const string ReadyTag = "ready";
        private const string StartupTag = "startup";

        [HttpGet("live")]
        [EndpointDescription("Verifica se a API está ativa")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status503ServiceUnavailable)]
        public Task<ContentResult> Liveness(CancellationToken cancellationToken) =>
            CheckHealthAsync(LiveTag, cancellationToken);

        [HttpGet("ready")]
        [EndpointDescription("Verifica se a API está pronta para receber requisições")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status503ServiceUnavailable)]
        public Task<ContentResult> Readiness(CancellationToken cancellationToken) =>
            CheckHealthAsync(ReadyTag, cancellationToken);

        [HttpGet("startup")]
        [EndpointDescription("Verifica se a inicialização da API foi concluída")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status503ServiceUnavailable)]
        public Task<ContentResult> Startup(CancellationToken cancellationToken) =>
            CheckHealthAsync(StartupTag, cancellationToken);

        private async Task<ContentResult> CheckHealthAsync(string tag, CancellationToken cancellationToken)
        {
            var report = await HealthCheckService.CheckHealthAsync(
                registration => registration.Tags.Contains(tag),
                cancellationToken);

            var statusCode = report.Status == HealthStatus.Unhealthy
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status200OK;

            return new ContentResult
            {
                Content = report.Status.ToString(),
                ContentType = "text/plain",
                StatusCode = statusCode
            };
        }
    }
}
