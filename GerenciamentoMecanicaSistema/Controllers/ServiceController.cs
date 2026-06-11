using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Service.Interface.Dto.Service;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class ServiceController(IMechanicalServiceService catalogService) : ControllerBase
    {
        private IMechanicalServiceService CatalogService { get; set; } = catalogService;

        [HttpPost("RegisterService")]
        public async Task<IActionResult> RegisterService([FromBody] CreateServiceDto serviceDto)
        {
            await CatalogService.RegisterService(serviceDto);

            return Ok();
        }

        [HttpGet("GetServices")]
        public async Task<OkObjectResult> GetServices()
        {
            var services = await CatalogService.GetServices();

            return Ok(services);
        }

        [HttpGet("GetService/{serviceId}")]
        public async Task<IActionResult> GetService([FromRoute] Guid serviceId)
        {
            var service = await CatalogService.GetService(serviceId);

            return Ok(service);
        }

        [HttpPatch("UpdateService")]
        public async Task<IActionResult> UpdateService([FromBody] ServiceDto serviceDto)
        {
            await CatalogService.UpdateService(serviceDto);

            return Ok();
        }

        [HttpDelete("DeleteService/{serviceId}")]
        public async Task<IActionResult> DeleteService([FromRoute] Guid serviceId)
        {
            await CatalogService.DeleteService(serviceId);

            return Ok();
        }
    }
}
