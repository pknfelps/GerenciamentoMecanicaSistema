using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto.User;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController(IAuthenticationService authenticationService) : ControllerBase
    {
        private IAuthenticationService AuthenticationService { get; set; } = authenticationService;

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] CreateUserDto userDto)
        {
            var token = await AuthenticationService.Authenticate(userDto);

            if (string.IsNullOrEmpty(token))
                return Unauthorized("Usuário ou senha inválidos");

            return Ok(token);
        }
    }
}
