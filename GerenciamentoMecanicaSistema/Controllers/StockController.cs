using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto.CustomAttributes;
using Service.Interface.Dto.Stock;

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
        public async Task<IActionResult> RegisterItem([FromBody] CreatePartDto itemDto)
        {
            await StockService.RegisterNewPart(itemDto);

            return Created();
        }

        [HttpGet()]
        [EndpointDescription("Endpoint para listar os itens do estoque")]
        [ProducesResponseType(typeof(IEnumerable<PartDto>), StatusCodes.Status200OK, Description = "Retorna a lista de itens no estoque")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        public async Task<OkObjectResult> GetItems([FromQuery] Guid? id = null, [FromQuery] string name = "", [FromQuery] string brand = "")
        {
            var itens = await StockService.GetParts(id, name, brand);

            return Ok(itens);
        }

        [HttpPost("amount/{id}")]
        [EndpointDescription("Endpoint para adicionar quantidade a um item")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> AddItemAmount([FromRoute, GuidValidation] Guid id, [FromBody] int value)
        {
            await StockService.AddPartAmount(id, value);

            return Ok();
        }

        [HttpPatch("amount/{id}")]
        [EndpointDescription("Endpoint para remover quantidade de um item")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RemoveItemAmount([FromRoute, GuidValidation] Guid id, [FromBody] int value)
        {
            await StockService.RemovePartAmount(id, value);

            return NoContent();
        }

        [HttpPatch("price/{id}")]
        [EndpointDescription("Endpoint para atualizar o preço de um item")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> UpdateItemPrice([FromRoute, GuidValidation] Guid id, [FromBody] double value)
        {
            await StockService.UpdatePartPrice(id, value);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [EndpointDescription("Endpoint para deletar um item")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> DeleteItem([FromRoute, GuidValidation] Guid id)
        {
            await StockService.DeletePart(id);

            return NoContent();
        }
    }
}
