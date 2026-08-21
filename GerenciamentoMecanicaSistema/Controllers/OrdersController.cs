using GerenciamentoMecanicaSistema.Contracts.Requests.Order;
using GerenciamentoMecanicaSistema.Contracts.Responses.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using GerenciamentoMecanicaSistema.Contracts.Validation;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class OrdersController(IOrdersService orderService) : ControllerBase
    {
        private IOrdersService OrderService { get; set; } = orderService;

        [HttpPost()]
        [EndpointDescription("Endpoint para registrar uma ordem de serviço")]
        [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest orderToCreate)
        {
            var orderId = await OrderService.CreateServiceOrder(
                orderToCreate.Customer.ToCommand(),
                orderToCreate.Vehicle.ToCommand(),
                [.. orderToCreate.Services?.Select(service => service.ToCommand()) ?? []],
                [.. orderToCreate.Materials?.Select(material => material.ToCommand()) ?? []]);

            return CreatedAtAction(nameof(CreateOrder), new { id = orderId }, new CreateOrderResponse(orderId));
        }

        [AllowAnonymous]
        [HttpGet("{id}/status")]
        [EndpointDescription("Endpoint para consultar a situação atual de uma ordem de serviço")]
        [ProducesResponseType(typeof(OrderStatusResponse), StatusCodes.Status200OK, Description = "Retorna a situação atual da ordem")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Identificação da ordem inválida")]
        [ProducesResponseType(StatusCodes.Status404NotFound, Description = "Ordem não encontrada")]
        public async Task<OkObjectResult> GetOrderStatus([FromRoute, GuidValidation] Guid id)
        {
            var status = await OrderService.GetOrderStatus(id);

            return Ok(new OrderStatusResponse(id, status.ToString()));
        }

        [HttpGet("operational")]
        [EndpointDescription("Endpoint para listar as ordens de serviço ativas por prioridade operacional")]
        [ProducesResponseType(typeof(IEnumerable<WorkOrderResponse>), StatusCodes.Status200OK, Description = "Retorna as ordens ativas ordenadas por prioridade e antiguidade")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        public async Task<OkObjectResult> GetOperationalOrders()
        {
            var orders = await OrderService.GetOperationalOrders();

            return Ok(orders.Select(WorkOrderResponse.Create));
        }

        [HttpGet("details")]
        [EndpointDescription("Endpoint para listar as ordens de serviço detalhadas")]
        [ProducesResponseType(typeof(IEnumerable<DetailedWorkOrderResponse>), StatusCodes.Status200OK, Description = "Retorna a lista de ordens")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        public async Task<OkObjectResult> GetDetailedOrders([FromQuery] Guid? id = null, [FromQuery] string vehicleLicensePlate = "")
        {
            var orders = await OrderService.GetOrders(id: id, vehicleLicensePlate: vehicleLicensePlate);

            return Ok(orders.Select(DetailedWorkOrderResponse.Create));
        }

        [HttpPatch("{id}/diagnosis/start")]
        [EndpointDescription("Endpoint para iniciar o diagnóstico da ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent, Description = "Retorna todas as ordens detalhadas do cliente")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        [ProducesResponseType(StatusCodes.Status404NotFound, Description = "Ordem não encontrada para o cliente")]
        public async Task<IActionResult> StartDiagnosis([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.StartDiagnosis(id);

            return NoContent();
        }

        [HttpPost("{id}/services")]
        [EndpointDescription("Endpoint para adicionar serviços a uma ordem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> AddServiceToOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateOrderItemRequest<int> service)
        {
            await OrderService.AddServiceToOrder(id, service.ToCommand());

            return Ok();
        }

        [HttpPatch("{id}/services")]
        [EndpointDescription("Endpoint para remover serviços de uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RemoveServiceOfOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateOrderItemRequest<int> service)
        {
            await OrderService.RemoveServiceOfOrder(id, service.ToCommand());

            return NoContent();
        }

        [HttpPost("{id}/materials")]
        [EndpointDescription("Endpoint para adicionar itens a uma ordem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> AddMaterialToOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateOrderItemRequest<int> orderItem)
        {
            await OrderService.AddMaterialToOrder(id, orderItem.ToCommand());

            return Ok();
        }

        [HttpPatch("{id}/materials")]
        [EndpointDescription("Endpoint para remover itens de uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RemoveMaterialOrSupplyFromOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateOrderItemRequest<int> orderItem)
        {
            await OrderService.RemoveMaterialFromOrder(id, orderItem.ToCommand());

            return NoContent();
        }

        [HttpPatch("{id}/diagnosis/complete")]
        [EndpointDescription("Endpoint para completar o diagnóstico de uma ordem, gerar o orçamento e notificar o cliente")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> CompleteDiagnosis([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.CompleteDiagnosis(id);

            return NoContent();
        }

        [AllowAnonymous]
        [HttpPatch("{id}/budget")]
        [EndpointDescription("Endpoint para aprovar ou recusar o orçamento de uma ordem. Não requer autenticação JWT para que o cliente possa aprovar sem a necessidade de um login. Autenticação será feita através do documento do cliente no corpo da request")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> ApproveBudget([FromRoute, GuidValidation] Guid id, [FromBody] ApproveOrderRequest approveOrder)
        {
            await OrderService.ApproveBudget(id, approveOrder.ToCommand());

            return NoContent();
        }

        [HttpPatch("{id}/execution/start")]
        [EndpointDescription("Endpoint para iniciar a execução de uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> StartExecution([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.StartExecution(id);

            return NoContent();
        }

        [HttpPatch("{id}/execution/complete")]
        [EndpointDescription("Endpoint para completar a execução de uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> CompleteExecution([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.CompleteExecution(id);

            return NoContent();
        }

        [HttpPatch("{id}/delivery")]
        [EndpointDescription("Endpoint para definir o veículo como entregue e finalizar a ordem por completo")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> VehicleDelivered([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.DeliverVehicle(id);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [EndpointDescription("Endpoint para deletar uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> DeleteOrder([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.DeleteOrder(id);

            return NoContent();
        }
    }
}
