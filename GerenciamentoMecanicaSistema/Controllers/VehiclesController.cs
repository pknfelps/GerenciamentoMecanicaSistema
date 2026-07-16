using GerenciamentoMecanicaSistema.Contracts.Requests.Vehicle;
using GerenciamentoMecanicaSistema.Contracts.Responses.Vehicle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using GerenciamentoMecanicaSistema.Contracts.Validation;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class VehiclesController(IVehicleService vehicleService) : ControllerBase
    {
        private IVehicleService VehicleService { get; set; } = vehicleService;

        [HttpPost()]
        [EndpointDescription("Endpoint para registrar um veículo")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RegisterVehicle([FromBody] CreateVehicleRequest vehicle)
        {
            await VehicleService.RegisterVehicle(vehicle.ToCommand());

            return Created();
        }

        [HttpGet()]
        [EndpointDescription("Endpoint para listar os veículos")]
        [ProducesResponseType(typeof(IEnumerable<VehicleResponse>), StatusCodes.Status200OK, Description = "Retorna a lista de veículos")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        public async Task<IActionResult> GetVehicles([FromQuery] Guid? id = null, [FromQuery] string licensePlate = "")
        {
            var vehicles = await VehicleService.GetVehicles(id, licensePlate);

            return Ok(vehicles.Select(VehicleResponse.Create));
        }

        [HttpPatch("{id}")]
        [EndpointDescription("Endpoint para atualizar os dados de um veículo")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> UpdateVehicle([FromRoute, GuidValidation] Guid id, [FromBody] CreateVehicleRequest vehicle)
        {
            await VehicleService.UpdateVehicle(id, vehicle.ToCommand());

            return NoContent();
        }

        [HttpDelete("{id}")]
        [EndpointDescription("Endpoint para deletar um veículo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> DeleteVehicle([FromRoute, GuidValidation] Guid id)
        {
            await VehicleService.DeleteVehicle(id);

            return Ok();
        }
    }
}
