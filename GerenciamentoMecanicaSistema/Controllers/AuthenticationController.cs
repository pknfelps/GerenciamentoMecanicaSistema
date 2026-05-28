using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController(IAuthenticationService authenticationService) : ControllerBase
    {
        private IAuthenticationService AuthenticationService { get; set; } = authenticationService;

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UsuarioDto usuarioDto)
        {
            var token = await AuthenticationService.Login(usuarioDto);

            if (string.IsNullOrEmpty(token))
                return Unauthorized("Usuário ou senha inválidos");

            return Ok(token);
        }
    }
}
