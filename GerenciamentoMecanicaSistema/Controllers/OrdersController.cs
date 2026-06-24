using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto;
using Service.Interface.Dto.CustomAttributes;
using Service.Interface.Dto.Order;

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
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto orderToCreate)
        {
            await OrderService.CreateServiceOrder(orderToCreate);

            return Created();
        }

        [HttpGet()]
        [EndpointDescription("Endpoint para listar as ordens de serviço")]
        [ProducesResponseType(typeof(IEnumerable<WorkOrderDto>), StatusCodes.Status200OK, Description = "Retorna a lista de ordens")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        public async Task<OkObjectResult> GetOrders([FromQuery] Guid? id = null, [FromQuery] string vehicleLicensePlate = "")
        {
            var orders = await OrderService.GetOrders(id: id, vehicleLicensePlate: vehicleLicensePlate);

            return Ok(orders.Select(WorkOrderDto.Create));
        }

        [HttpGet("details")]
        [EndpointDescription("Endpoint para listar as ordens de serviço detalhadas")]
        [ProducesResponseType(typeof(IEnumerable<DetailedWorkOrderDto>), StatusCodes.Status200OK, Description = "Retorna a lista de ordens")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        public async Task<OkObjectResult> GetDetailedOrders([FromQuery] Guid? id = null, [FromQuery] string vehicleLicensePlate = "")
        {
            var orders = await OrderService.GetOrders(id: id, vehicleLicensePlate: vehicleLicensePlate);

            return Ok(orders);
        }

        [AllowAnonymous]
        [HttpGet("vehicles/{licensePlate}")]
        [EndpointDescription("Endpoint para exibir as ordens detalhadas de um veículo. Não requer autenticação JWT para que possa ser acessado pelos clientes para acompanhar a ordem.")]
        [ProducesResponseType(typeof(DetailedWorkOrderDto), StatusCodes.Status200OK, Description = "Retorna todas as ordens detalhadas do cliente")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> GetVehicleOrders([FromRoute, RegularLicensePlateExpression] string licensePlate)
        {
            var order = await OrderService.GetOrders(vehicleLicensePlate: licensePlate);

            return Ok(order);
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
        public async Task<IActionResult> AddServiceToOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateItemDto<int> service)
        {
            await OrderService.AddServiceToOrder(id, service);

            return Ok();
        }

        [HttpPatch("{id}/services")]
        [EndpointDescription("Endpoint para remover serviços de uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RemoveServiceOfOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateItemDto<int> service)
        {
            await OrderService.RemoveServiceOfOrder(id, service);

            return NoContent();
        }

        [HttpPost("{id}/materials")]
        [EndpointDescription("Endpoint para adicionar itens a uma ordem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> AddMaterialToOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateItemDto<int> orderItem)
        {
            await OrderService.AddMaterialToOrder(id, orderItem);

            return Ok();
        }

        [HttpPatch("{id}/materials")]
        [EndpointDescription("Endpoint para remover itens de uma ordem")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Token de autenticação inválido")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Request mal formado")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Description = "Erro interno do servidor")]
        public async Task<IActionResult> RemoveMaterialOrSupplieFromOrder([FromRoute, GuidValidation] Guid id, [FromBody] UpdateItemDto<int> orderItem)
        {
            await OrderService.RemoveMaterialFromOrder(id, orderItem);

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
        public async Task<IActionResult> ApproveBudget([FromRoute, GuidValidation] Guid id, [FromBody] ApproveOrderDto approveOrder)
        {
            await OrderService.ApproveBudget(id, approveOrder);

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
