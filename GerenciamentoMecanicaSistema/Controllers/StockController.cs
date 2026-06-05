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
        public async Task<IActionResult> RegisterItem([FromBody] StockItemDto itemDto)
        {
            Console.WriteLine("Requisitando criação do item");
            await StockService.RegisterNewItem(itemDto);

            Console.WriteLine("Item criado");
            return Created();
        }

        [HttpGet("GetItens")]
        public async Task<OkObjectResult> GetItens()
        {
            Console.WriteLine("Requisitando itens");
            var itens = await StockService.GetItens();

            Console.WriteLine("Retornando itens");
            return Ok(itens);
        }

        [HttpGet("GetItem/{name}/{brand}")]
        public async Task<IActionResult> GetItem([FromRoute, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")] string name, [FromRoute, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")] string brand)
        {
            Console.WriteLine("Requisitando item");
            var item = await StockService.GetItem(name, brand);

            if (item == null)
            {
                Console.WriteLine("Item não encontrado");
                return NotFound("Item não encontrado");
            }

            Console.WriteLine("Retornando item");
            return Ok(item);
        }

        [HttpPatch("AddItemAmount")]
        public async Task<IActionResult> AddItemAmount([FromBody] StockItemUpdateDto<int> itemDto)
        {
            Console.WriteLine("Rquisitando adição do item");
            await StockService.AddItemAmount(itemDto);

            Console.WriteLine("Adição concluida com sucesso");
            return Ok();
        }

        [HttpPatch("RemoveItemAmount")]
        public async Task<IActionResult> RemoveItemAmount([FromBody] StockItemUpdateDto<int> itemDto)
        {
            Console.WriteLine("Requisitando remoção do item");
            await StockService.RemoveItemAmount(itemDto);

            Console.WriteLine("Remoção concluida com sucesso");
            return Ok();
        }

        [HttpPatch("ReserveItemAmount")]
        public async Task<IActionResult> ReserveItemAmount([FromBody] StockItemUpdateDto<int> itemDto)
        {
            Console.WriteLine("Requisitando reserva do item");
            await StockService.ReserveItemAmount(itemDto);

            Console.WriteLine("Reserva realizada com sucesso");
            return Ok();
        }

        [HttpPatch("RestoreItemAmount")]
        public async Task<IActionResult> RestoreItemAmount([FromBody] StockItemUpdateDto<int> itemDto)
        {
            Console.WriteLine("Requisitando restauração do itten");
            await StockService.RestoreItemAmount(itemDto);

            Console.WriteLine("Item restaurado com sucesso");
            return Ok();
        }

        [HttpPatch("UpdateItemPrice")]
        public async Task<IActionResult> UpdateItemPrice([FromBody] StockItemUpdateDto<double> itemDto)
        {
            Console.WriteLine("Requisitando atualização do preço");
            await StockService.UpdateItemPrice(itemDto);

            Console.WriteLine("Preço atualizado com sucesso");
            return Ok();
        }

        [HttpDelete("DeleteItem/{name}/{brand}")]
        public async Task<IActionResult> DeleteItem([FromRoute, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")] string name, [FromRoute, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")] string brand)
        {
            Console.WriteLine("Requisitando deleção do iten");
            await StockService.DeleteItem(name, brand);

            Console.WriteLine("Item deletado com sucesso");
            return Ok();
        }
    }
}
