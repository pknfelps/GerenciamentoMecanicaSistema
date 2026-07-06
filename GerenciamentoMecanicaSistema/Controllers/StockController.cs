using GerenciamentoMecanicaSistema.Contracts.Requests.Stock;
using GerenciamentoMecanicaSistema.Contracts.Responses.Stock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto.CustomAttributes;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class StockController(IStockService stockService) : ControllerBase
    {
        public IStockService StockService { get; private set; } = stockService;

        [HttpPost()]
        [EndpointDescription("Endpoint para registrar o item no estoque")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RegisterMaterial([FromBody] CreateMaterialRequest item)
        {
            await StockService.RegisterNewMaterial(item.ToCommand());

            return Created();
        }

        [HttpGet()]
        [EndpointDescription("Endpoint para listar os itens do estoque")]
        [ProducesResponseType(typeof(IEnumerable<MaterialResponse>), StatusCodes.Status200OK, Description = "Retorna a lista de itens no estoque")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        public async Task<OkObjectResult> GetMaterials([FromQuery] Guid? id = null, [FromQuery] string name = "", [FromQuery] string brand = "")
        {
            var itens = await StockService.GetMaterials(id, name, brand);

            return Ok(itens.Select(MaterialResponse.Create));
        }

        [HttpPost("amount/{id}")]
        [EndpointDescription("Endpoint para adicionar quantidade a um item")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> AddMaterialAmount([FromRoute, GuidValidation] Guid id, [FromBody] ValueUpdateRequest<int> value)
        {
            await StockService.AddMaterialAmount(id, value.Value);

            return Ok();
        }

        [HttpPatch("amount/{id}")]
        [EndpointDescription("Endpoint para remover quantidade de um item")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RemoveMaterialAmount([FromRoute, GuidValidation] Guid id, ValueUpdateRequest<int> value)
        {
            await StockService.RemoveMaterialAmount(id, value.Value);

            return NoContent();
        }

        [HttpPatch("price/{id}")]
        [EndpointDescription("Endpoint para atualizar o preço de um item")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> UpdateMaterialPrice([FromRoute, GuidValidation] Guid id, ValueUpdateRequest<double> value)
        {
            await StockService.UpdateMaterialPrice(id, value.Value);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [EndpointDescription("Endpoint para deletar um item")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> DeleteMaterial([FromRoute, GuidValidation] Guid id)
        {
            await StockService.DeleteMaterial(id);

            return NoContent();
        }
    }
}
