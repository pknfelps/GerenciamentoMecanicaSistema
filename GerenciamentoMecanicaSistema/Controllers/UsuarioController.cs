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
    public class UsuarioController(IUsuarioService usuarioService) : ControllerBase
    {
        private IUsuarioService UsuarioService { get; set; } = usuarioService;

        [HttpPost("RegisterUsuario")]
        public async Task<IActionResult> CreateUsuario([FromBody] UsuarioDto usuarioDto)
        {
            Console.WriteLine("Requisitando criação do usuario");
            await UsuarioService.RegisterUsuario(usuarioDto);
            Console.WriteLine("Usuário criado");

            return Created();
        }

        [HttpGet("GetUsuarioByNomeAndCargo/{nome}/{cargo}")]
        public async Task<IActionResult> GetUsuarioByNomeAndCargo(
            [FromRoute][RegularExpression(@"^[a-zA-ZÀ-ÿ]{2,}$", ErrorMessage = "Nome inválido")] string nome, 
            [FromRoute][RegularExpression(@"^[a-zA-ZÀ-ÿ]{2,}$", ErrorMessage = "Cargo inválido")] string cargo)
        {
            Console.WriteLine($"Requisitando usuario com nome {nome}");
            var usuario = await UsuarioService.GetUsuario(new UsuarioDto(nome, "", cargo));

            if (usuario is null)
                return NotFound();

            return Ok(usuario);
        }
    }
}
