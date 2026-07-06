using GerenciamentoMecanicaSistema.Contracts.Requests.User;
using GerenciamentoMecanicaSistema.Contracts.Responses.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Commands.User;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController(IUserService usuarioService) : ControllerBase
    {
        private IUserService UsersService { get; set; } = usuarioService;

        [HttpPost()]
        [EndpointDescription("Endpoint para registrar um usuário")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RegisterUser([FromBody] CreateUserRequest user)
        {
            await UsersService.RegisterUser(user.ToCommand());

            return Created();
        }

        [HttpGet()]
        [EndpointDescription("Endpoint para exibir um usuário")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK, Description = "Retorna o usuário")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        [ProducesResponseType(StatusCodes.Status404NotFound, Description = "Usuário não encontrado")]
        public async Task<IActionResult> GetUser([FromQuery] string name, [FromQuery] string role)
        {
            var usuario = await UsersService.GetUser(new CreateUserCommand(name, "", role));

            if (usuario is null)
                return NotFound();

            return Ok(UserResponse.Create(usuario));
        }
    }
}
