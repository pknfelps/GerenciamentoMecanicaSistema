using GerenciamentoMecanicaSistema.Contracts.Requests.Customer;
using GerenciamentoMecanicaSistema.Contracts.Responses.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using GerenciamentoMecanicaSistema.Contracts.Validation;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class CustomersController(ICustomerService customerService) : ControllerBase
    {
        private ICustomerService CustomerService { get; set; } = customerService;

        [HttpPost()]
        [EndpointDescription("Endpoint para registrar um cliente")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Corpo da request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RegisterCustomer([FromBody] CreateCustomerRequest customer)
        {
            await CustomerService.RegisterCustomer(customer.ToCommand());

            return Created();
        }

        [HttpGet()]
        [EndpointDescription("Endpoint para listar os clientes")]
        [ProducesResponseType(typeof(IEnumerable<CustomerResponse>), StatusCodes.Status200OK, Description = "Retorna a lista de clientes")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        public async Task<OkObjectResult> GetCustomers([FromQuery] Guid? id = null, [FromQuery] string name = "", [FromQuery] string document = "")
        {
            var customers = await CustomerService.GetCustomers(id, name, document);

            return Ok(customers.Select(CustomerResponse.Create));
        }

        [HttpPatch("{id}")]
        [EndpointDescription("Endpoint para atualizar os dados de um cliente")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> UpdateCustomer([FromRoute, GuidValidation] Guid id, [FromBody] CreateCustomerRequest customer)
        {
            await CustomerService.UpdateCustomer(id, customer.ToCommand());

            return NoContent();
        }

        [HttpDelete("{id}")]
        [EndpointDescription("Endpoint para deletar um cliente")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> DeleteCustomer([FromRoute, GuidValidation] Guid id)
        {
            await CustomerService.DeleteCustomer(id);

            return NoContent();
        }
    }
}
