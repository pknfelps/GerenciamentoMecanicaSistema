using DTOs;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClienteController(IClienteService clienteService) : ControllerBase
    {
        private IClienteService ClienteService { get; set; } = clienteService;

        [HttpPost("CreateCliente")]
        public async Task<IActionResult> CreateCliente([FromBody] ClienteDto clienteDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Console.WriteLine("Requisitando criação do cliente");
            await ClienteService.CreateCliente(clienteDto);
            Console.WriteLine("Cliente criado");

            return Created();
        }

        [HttpGet("GetClientes")]
        public async Task<OkObjectResult> GetClientes()
        {
            Console.WriteLine("Requisitando lista de clientes");
            var clientes = await ClienteService.GetClientes();

            return Ok(clientes);
        }

        [HttpGet("GetClienteByDocumento/{documento}")]
        public async Task<IActionResult> GetClienteByDocumento([FromRoute][RegularExpression(@"^(\d{11}|\d{14})$", ErrorMessage = "Documento inválido")] string documento)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Console.WriteLine($"Requisitando cliente com documento {documento}");
            var cliente = await ClienteService.GetClienteByDocumento(documento);

            if (cliente is null)
                return NotFound();

            return Ok(cliente);
        }

        [HttpPatch("UpdateCliente")]
        public async Task<IActionResult> UpdateCliente([FromBody] ClienteDto clienteDto)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            Console.WriteLine($"Requisitando atualização do cliente");
            await ClienteService.UpdateCliente(clienteDto);
            Console.WriteLine($"Cliente atualizado");

            return Ok();
        }

        [HttpDelete("DeleteCliente/{documento}")]
        public async Task<IActionResult> DeleteCliente([FromRoute][RegularExpression(@"^(\d{11}|\d{14})$", ErrorMessage = "Documento inválido")] string documento)
        {
            Console.WriteLine($"Requisitando deleção do cliente de documento {documento}");
            await ClienteService.DeleteCliente(documento);
            Console.WriteLine($"Cliente deletado");

            return Ok();
        }
    }
}
