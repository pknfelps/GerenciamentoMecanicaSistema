using GerenciamentoMecanicaSistema.Contracts.Requests.User;
using GerenciamentoMecanicaSistema.Contracts.Responses.User;
using GerenciamentoMecanicaSistema.Contracts.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using GerenciamentoMecanicaSistema.Authorization;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class UsersController(IUserService usuarioService) : ControllerBase
    {
        private IUserService UsersService { get; set; } = usuarioService;

        [HttpPost()]
        [Authorize(Roles = "Admin")]
        [EndpointDescription("Endpoint para registrar um usuário")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RegisterUser([FromBody] CreateUserRequest user)
        {
            await UsersService.RegisterUser(user.ToCommand());

            return Created();
        }

        [HttpGet()]
        [Authorize(Roles = "Admin,Mechanic")]
        [EndpointDescription("Endpoint para consultar um usuário")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK, Description = "Retorna o usuário")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        [ProducesResponseType(StatusCodes.Status404NotFound, Description = "Usuário não encontrado")]
        public async Task<IActionResult> GetUser([FromQuery, RegularNonEmptyStringExpression] string name = "", [FromQuery, RegularNonEmptyStringExpression] string role = "")
        {
            Service.Interface.Results.User.UserResult? usuario;
            if (User.IsInRole("Admin"))
                usuario = await UsersService.GetUser(name, role);
            else
            {
                if (!AuthenticatedIdentity.TryGetId(User, out var userId))
                    return Challenge();
                usuario = await UsersService.GetOwnUser(userId, name, role);
            }

            if (usuario is null)
                return NotFound();

            return Ok(UserResponse.Create(usuario));
        }
    }
}
