using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class ClienteController(IClienteService clienteService) : ControllerBase
    {
        private IClienteService ClienteService { get; set; } = clienteService;

        [HttpPost("CreateCliente")]
        public async Task<IActionResult> CreateCliente([FromBody] ClienteDto clienteDto)
        {
            await ClienteService.CreateCliente(clienteDto);

            return Created();
        }

        [HttpGet("GetClientes")]
        public async Task<OkObjectResult> GetClientes()
        {
            var clientes = await ClienteService.GetClientes();

            return Ok(clientes);
        }

        [HttpGet("GetClienteByDocumento/{documento}")]
        public async Task<IActionResult> GetClienteByDocumento([FromRoute][RegularExpression(@"^(\d{11}|\d{14})$", ErrorMessage = "Documento inválido")] string documento)
        {
            var cliente = await ClienteService.GetClienteByDocumento(documento);

            if (cliente is null)
                return NotFound();

            return Ok(cliente);
        }

        [HttpPatch("UpdateCliente")]
        public async Task<IActionResult> UpdateCliente([FromBody] ClienteDto clienteDto)
        {
            await ClienteService.UpdateCliente(clienteDto);

            return Ok();
        }

        [HttpDelete("DeleteCliente/{documento}")]
        public async Task<IActionResult> DeleteCliente([FromRoute][RegularExpression(@"^(\d{11}|\d{14})$", ErrorMessage = "Documento inválido")] string documento)
        {
            await ClienteService.DeleteCliente(documento);

            return Ok();
        }
    }
}
