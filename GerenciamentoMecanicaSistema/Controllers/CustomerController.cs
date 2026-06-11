using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto.Customer;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class CustomerController(ICustomerService customerService) : ControllerBase
    {
        private ICustomerService CustomerService { get; set; } = customerService;

        [HttpPost("RegisterCustomer")]
        public async Task<IActionResult> RegisterCustomer([FromBody] CreateCustomerDto customerDto)
        {
            await CustomerService.RegisterCustomer(customerDto);

            return Created();
        }

        [HttpGet("GetCustomers")]
        public async Task<OkObjectResult> GetCustomers()
        {
            var costumer = await CustomerService.GetCustomers();

            return Ok(costumer);
        }

        [HttpGet("GetCustomer/{document}")]
        public async Task<IActionResult> GetCustomer([FromRoute][RegularExpression(@"^(\d{11}|\d{14})$", ErrorMessage = "Documento inválido")] string document)
        {
            var customer = await CustomerService.GetCustomer(document);

            if (customer is null)
                return NotFound();

            return Ok(customer);
        }

        [HttpPatch("UpdateCustomer")]
        public async Task<IActionResult> UpdateCustomer([FromBody] CustomerDto customerDto)
        {
            await CustomerService.UpdateCustomer(customerDto);

            return Ok();
        }

        [HttpDelete("DeleteCustomer/{document}")]
        public async Task<IActionResult> DeleteCustomer([FromRoute][RegularExpression(@"^(\d{11}|\d{14})$", ErrorMessage = "Documento inválido")] string document)
        {
            await CustomerService.DeleteCustomer(document);

            return Ok();
        }
    }
}
