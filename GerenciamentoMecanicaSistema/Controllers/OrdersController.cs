using GerenciamentoMecanicaSistema.Contracts.Requests.Order;
using GerenciamentoMecanicaSistema.Contracts.Responses.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using GerenciamentoMecanicaSistema.Contracts.Validation;
using GerenciamentoMecanicaSistema.Authorization;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class OrdersController(IOrdersService orderService) : ControllerBase
    {
        private IOrdersService OrderService { get; set; } = orderService;

        [HttpPost()]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para registrar uma ordem de serviço")]
        [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest orderToCreate)
        {
            var orderId = await OrderService.CreateServiceOrder(orderToCreate.ToCommand());

            return CreatedAtAction(nameof(CreateOrder), new { id = orderId }, new CreateOrderResponse(orderId));
        }

        [HttpGet("{id}/status")]
        [Authorize(Roles = "Mechanic,Customer")]
        [EndpointDescription("Endpoint para consultar o status de uma ordem de serviço")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token ou identidade inválida")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(typeof(OrderStatusResponse), StatusCodes.Status200OK, Description = "Retorna a situação atual da ordem")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Identificação da ordem inválida")]
        [ProducesResponseType(StatusCodes.Status404NotFound, Description = "Ordem não encontrada")]
        public async Task<IActionResult> GetOrderStatus([FromRoute, GuidValidation] Guid id)
        {
            Domain.Interface.Order.WorkOrderStatus status;
            if (User.IsInRole("Mechanic"))
                status = await OrderService.GetOrderStatus(id);
            else
            {
                if (!AuthenticatedIdentity.TryGetId(User, out var customerId))
                    return Challenge();
                status = await OrderService.GetCustomerOrderStatus(id, customerId);
            }

            return Ok(new OrderStatusResponse(id, status.ToString()));
        }

        [HttpGet("operational")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para listar as ordens de serviço ativas por prioridade operacional")]
        [ProducesResponseType(typeof(IEnumerable<WorkOrderResponse>), StatusCodes.Status200OK, Description = "Retorna as ordens ativas ordenadas por prioridade e antiguidade")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        public async Task<OkObjectResult> GetOperationalOrders()
        {
            var orders = await OrderService.GetOperationalOrders();

            return Ok(orders.Select(WorkOrderResponse.Create));
        }

        [HttpGet("details")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para listar as ordens de serviço detalhadas")]
        [ProducesResponseType(typeof(IEnumerable<DetailedWorkOrderResponse>), StatusCodes.Status200OK, Description = "Retorna a lista de ordens")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        public async Task<OkObjectResult> GetDetailedOrders([FromQuery] Guid? id = null, [FromQuery] string vehicleLicensePlate = "")
        {
            var orders = await OrderService.GetOrders(id: id, vehicleLicensePlate: vehicleLicensePlate);

            return Ok(orders.Select(DetailedWorkOrderResponse.Create));
        }

        [HttpPatch("{id}/diagnosis/start")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para iniciar o diagnóstico da ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent, Description = "Diagnóstico iniciado com sucesso")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        [ProducesResponseType(StatusCodes.Status404NotFound, Description = "Ordem não encontrada para o cliente")]
        public async Task<IActionResult> StartDiagnosis([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.StartDiagnosis(id);

            return NoContent();
        }

        [HttpPost("{id}/services")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para adicionar serviços a uma ordem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> AddServiceToOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateOrderItemRequest<int> service)
        {
            await OrderService.AddServiceToOrder(id, service.ToCommand());

            return Ok();
        }

        [HttpPatch("{id}/services")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para remover serviços de uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RemoveServiceOfOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateOrderItemRequest<int> service)
        {
            await OrderService.RemoveServiceOfOrder(id, service.ToCommand());

            return NoContent();
        }

        [HttpPost("{id}/materials")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para adicionar itens a uma ordem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> AddMaterialToOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateOrderItemRequest<int> orderItem)
        {
            await OrderService.AddMaterialToOrder(id, orderItem.ToCommand());

            return Ok();
        }

        [HttpPatch("{id}/materials")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para remover itens de uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RemoveMaterialOrSupplyFromOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateOrderItemRequest<int> orderItem)
        {
            await OrderService.RemoveMaterialFromOrder(id, orderItem.ToCommand());

            return NoContent();
        }

        [HttpPatch("{id}/diagnosis/complete")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para completar o diagnóstico de uma ordem, gerar o orçamento e notificar o cliente")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> CompleteDiagnosis([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.CompleteDiagnosis(id);

            return NoContent();
        }

        [HttpPatch("{id}/budget")]
        [Authorize(Roles = "Customer")]
        [EndpointDescription("Endpoint para aprovar ou recusar o orçamento de uma ordem de serviço")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token ou identidade inválida")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status404NotFound, Description = "Ordem não encontrada")]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Description = "Documento divergente ou estado incompatível")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> ApproveBudget([FromRoute, GuidValidation] Guid id, [FromBody] ApproveOrderRequest approveOrder)
        {
            if (!AuthenticatedIdentity.TryGetId(User, out var customerId))
                return Challenge();

            await OrderService.ApproveBudget(id, approveOrder.ToCommand(), customerId);

            return NoContent();
        }

        [HttpPatch("{id}/execution/start")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para iniciar a execução de uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> StartExecution([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.StartExecution(id);

            return NoContent();
        }

        [HttpPatch("{id}/execution/complete")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para completar a execução de uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> CompleteExecution([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.CompleteExecution(id);

            return NoContent();
        }

        [HttpPatch("{id}/delivery")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para definir o veículo como entregue e finalizar a ordem por completo")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> VehicleDelivered([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.DeliverVehicle(id);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Mechanic")]
        [EndpointDescription("Endpoint para excluir uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Perfil sem permissão para esta operação")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Requisição malformada")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> DeleteOrder([FromRoute, GuidValidation] Guid id)
        {
            await OrderService.DeleteOrder(id);

            return NoContent();
        }
    }
}
