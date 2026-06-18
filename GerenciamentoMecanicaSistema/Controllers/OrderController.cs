using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto.CustomAttributes;
using Service.Interface.Dto.Order;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class OrderController(IWorkOrderService orderService) : ControllerBase
    {
        private IWorkOrderService OrderService { get; set; } = orderService;

        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto orderToCreate)
        {
            await OrderService.CreateServiceOrder(orderToCreate);

            return Created();
        }

        [HttpGet("GetOrders")]
        public async Task<OkObjectResult> GetOrders()
        {
            var orders = await OrderService.GetOrders();

            return Ok(orders);
        }

        [HttpGet("GetOrder/{id}")]
        public async Task<IActionResult> GetOrder([FromRoute] Guid id)
        {
            var order = await OrderService.GetOrder(id);

            if (order == null)
                return NotFound("Ordem não encontrada");

            return Ok(order);
        }

        [AllowAnonymous]
        [HttpGet("GetCustomerOrders/{document}")]
        public async Task<IActionResult> GetCustomerOrders([FromRoute, RegularDocumentExpression] string document)
        {
            var order = await OrderService.GetCustomerOrders(document);

            if (order == null)
                return NotFound($"Ordem não encontrada para o cliente de documento \"{document}\"");

            return Ok(order);
        }

        [HttpPost("StartDiagnosis/{orderId}")]
        public async Task<IActionResult> StartDiagnosis([FromRoute] Guid orderId)
        {
            await OrderService.StartDiagnosis(orderId);

            return Ok();
        }

        [HttpPost("AddServiceToOrder")]
        public async Task<IActionResult> AddServiceToOrder([FromBody] OrderUpdateItemDto service)
        {
            await OrderService.AddServiceToOrder(service);

            return Ok();
        }

        [HttpPost("RemoveServiceOfOrder")]
        public async Task<IActionResult> RemoveServiceOfOrder([FromBody] OrderUpdateItemDto service)
        {
            await OrderService.RemoveServiceOfOrder(service);

            return Ok();
        }

        [HttpPost("AddPartOrSupplieToOrder")]
        public async Task<IActionResult> AddPartOrSupplieToOrder([FromBody] OrderUpdateItemDto orderItem)
        {
            await OrderService.AddPartOrSupplieToOrder(orderItem);

            return Ok();
        }

        [HttpPost("RemovePartOrSupplieFromOrder")]
        public async Task<IActionResult> RemovePartOrSupplieFromOrder([FromBody] OrderUpdateItemDto orderItem)
        {
            await OrderService.RemovePartOrSupplieFromOrder(orderItem);

            return Ok();
        }

        [HttpPost("CompleteDiagnosis/{orderId}")]
        public async Task<IActionResult> CompleteDiagnosis([FromRoute] Guid orderId)
        {
            await OrderService.CompleteDiagnosis(orderId);

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("ApproveBudget")]
        public async Task<IActionResult> ApproveBudget([FromBody] ApproveOrderDto approve)
        {
            await OrderService.ApproveBudget(approve);

            return Ok();
        }

        [HttpPost("StartExecution/{orderId}")]
        public async Task<IActionResult> StartExecution([FromRoute] Guid orderId)
        {
            await OrderService.StartExecution(orderId);

            return Ok();
        }

        [HttpPost("CompleteExecution/{orderId}")]
        public async Task<IActionResult> CompleteExecution([FromRoute] Guid orderId)
        {
            await OrderService.CompleteExecution(orderId);

            return Ok();
        }

        [HttpPost("VehicleDelivered/{orderId}")]
        public async Task<IActionResult> VehicleDelivered([FromRoute] Guid orderId)
        {
            await OrderService.VehicleDelivered(orderId);

            return Ok();
        }

        [HttpDelete("DeleteOrder/{id}")]
        public async Task<IActionResult> DeleteOrder([FromRoute] Guid id)
        {
            await OrderService.DeleteOrder(id);

            return Ok();
        }
    }
}
