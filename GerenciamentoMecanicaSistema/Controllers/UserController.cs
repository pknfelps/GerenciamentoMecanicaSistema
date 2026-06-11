using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto.User;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserController(IUserService usuarioService) : ControllerBase
    {
        private IUserService UsuarioService { get; set; } = usuarioService;

        [HttpPost("RegisterUser")]
        public async Task<IActionResult> RegisterUser([FromBody] CreateUserDto userDto)
        {
            await UsuarioService.RegisterUser(userDto);

            return Created();
        }

        [HttpGet("GetUser/{name}/{role}")]
        public async Task<IActionResult> GetUser(
            [FromRoute][RegularExpression(@"^[a-zA-ZÀ-ÿ]{2,}$", ErrorMessage = "Nome inválido")] string name, 
            [FromRoute][RegularExpression(@"^[a-zA-ZÀ-ÿ]{2,}$", ErrorMessage = "Cargo inválido")] string role)
        {
            var usuario = await UsuarioService.GetUser(new CreateUserDto(name, "", role));

            if (usuario is null)
                return NotFound();

            return Ok(usuario);
        }
    }
}
