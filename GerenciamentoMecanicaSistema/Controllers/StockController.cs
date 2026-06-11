using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto.Stock;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class StockController(IStockService stockService) : ControllerBase
    {
        public IStockService StockService { get; private set; } = stockService;

        [HttpPost("RegisterItem")]
        public async Task<IActionResult> RegisterItem([FromBody] CreatePartDto itemDto)
        {
            await StockService.RegisterNewPart(itemDto);

            return Created();
        }

        [HttpGet("GetItems")]
        public async Task<OkObjectResult> GetItems()
        {
            var itens = await StockService.GetParts();

            return Ok(itens);
        }

        [HttpGet("GetItem/{name}/{brand}")]
        public async Task<IActionResult> GetItem([FromRoute, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")] string name, [FromRoute, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")] string brand)
        {
            var item = await StockService.GetPart(name, brand);

            if (item == null)
                return NotFound("Item não encontrado");

            return Ok(item);
        }

        [HttpPatch("AddItemAmount")]
        public async Task<IActionResult> AddItemAmount([FromBody] PartUpdateDto<int> itemDto)
        {
            await StockService.AddPartAmount(itemDto);

            return Ok();
        }

        [HttpPatch("RemoveItemAmount")]
        public async Task<IActionResult> RemoveItemAmount([FromBody] PartUpdateDto<int> itemDto)
        {
            await StockService.RemovePartAmount(itemDto);

            return Ok();
        }

        [HttpPatch("UpdateItemPrice")]
        public async Task<IActionResult> UpdateItemPrice([FromBody] PartUpdateDto<double> itemDto)
        {
            await StockService.UpdatePartPrice(itemDto);

            return Ok();
        }

        [HttpDelete("DeleteItem/{name}/{brand}")]
        public async Task<IActionResult> DeleteItem([FromRoute, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")] string name, [FromRoute, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")] string brand)
        {
            await StockService.DeletePart(name, brand);

            return Ok();
        }
    }
}
