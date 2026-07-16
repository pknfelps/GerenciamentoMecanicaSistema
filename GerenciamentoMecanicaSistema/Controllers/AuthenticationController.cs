using GerenciamentoMecanicaSistema.Contracts.Requests.User;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController(IAuthenticationService authenticationService) : ControllerBase
    {
        private IAuthenticationService AuthenticationService { get; set; } = authenticationService;

        [HttpPost()]
        [EndpointDescription("Endpoint de authenticação do usuário")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK, Description = "Retorna o token de autenticação")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Usuário ou senha inválidos")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        public async Task<IActionResult> Login([FromBody] CreateUserRequest user)
        {
            var token = await AuthenticationService.Authenticate(user.ToCommand());

            if (string.IsNullOrEmpty(token))
                return Unauthorized("Usuário ou senha inválidos");

            return Ok(token);
        }
    }
}
