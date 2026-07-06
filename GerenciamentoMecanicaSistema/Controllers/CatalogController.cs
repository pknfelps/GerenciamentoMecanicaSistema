using GerenciamentoMecanicaSistema.Contracts.Requests.Catalog;
using GerenciamentoMecanicaSistema.Contracts.Responses.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using GerenciamentoMecanicaSistema.Contracts.Validation;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class CatalogController(ICatalogService catalogService) : ControllerBase
    {
        private ICatalogService CatalogService { get; set; } = catalogService;

        [HttpPost()]
        [EndpointDescription("Endpoint para registrar um serviço")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RegisterService([FromBody] CreateServiceRequest service)
        {
            await CatalogService.RegisterService(service.ToCommand());

            return Created();
        }

        [HttpGet()]
        [EndpointDescription("Endpoint para listar os serviços")]
        [ProducesResponseType(typeof(IEnumerable<ServiceResponse>), StatusCodes.Status200OK, Description = "Retorna a lista de serviços")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        public async Task<OkObjectResult> GetServices([FromQuery] Guid? id = null, [FromQuery] string description = "")
        {
            var services = await CatalogService.GetServices(id, description);

            return Ok(services.Select(ServiceResponse.Create));
        }

        [HttpPatch("{id}")]
        [EndpointDescription("Endpoint para atualizar um serviço")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> UpdateService([FromRoute, GuidValidation] Guid id, [FromBody] CreateServiceRequest service)
        {
            await CatalogService.UpdateService(id, service.ToCommand());

            return NoContent();
        }

        [HttpDelete("{serviceId}")]
        [EndpointDescription("Endpoint para deletar um serviço")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> DeleteService([FromRoute, GuidValidation] Guid serviceId)
        {
            await CatalogService.DeleteService(serviceId);

            return Ok();
        }
    }
}
