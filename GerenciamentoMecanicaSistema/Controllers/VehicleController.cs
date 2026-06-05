using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using Service.Interface;
using Service.Interface.Dto;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class VehicleController(IVehicleService vehicleService) : ControllerBase
    {
        private IVehicleService VehicleService { get; set; } = vehicleService;

        [HttpPost("RegisterVehicle")]
        public async Task<IActionResult> RegisterVehicle([FromBody] VehicleDto vehicleDto)
        {
            await VehicleService.RegisterVehicle(vehicleDto);

            return Created();
        }

        [HttpGet("GetVehicles")]
        public async Task<IActionResult> GetVehicles()
        {
            var vehicles = await VehicleService.GetVehicles();

            return Ok(vehicles);
        }

        [HttpGet("GetVehicle/{licensePlate}")]
        public async Task<IActionResult> GetVehicle([FromRoute][RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Placa inválida")] string licensePlate)
        {
            var vehicle = await VehicleService.GetVehicle(licensePlate);

            if (vehicle is null)
                return NotFound();

            return Ok(vehicle);
        }

        [HttpPatch("UpdateVehicle")]
        public async Task<IActionResult> UpdateVehicle([FromBody] VehicleDto vehicleDto)
        {
            await VehicleService.UpdateVehicle(vehicleDto);

            return Ok();
        }

        [HttpDelete("DeleteVehicle/{licensePlate}")]
        public async Task<IActionResult> DeleteVehicle([FromRoute][RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Placa inválida")] string licensePlate)
        {
            await VehicleService.DeleteVehicle(licensePlate);

            return Ok();
        }
    }
}
