using Domain.Interface.Service;
using Domain.MechanicalService;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Service
{
    public class ServiceDto(Guid id, string description, float hours, double pricePerHour, int amount) : CreateServiceDto(description, hours, pricePerHour, amount)
    {
        [Required, GuidValidation]
        public Guid Id { get; private set; } = id;

        public static ServiceDto Create(IMechanicalService service) => new(service.Id, service.Description, service.Hours, service.PricePerHour, service.Amount);

        public override IMechanicalService ToDomain() => new MechanicalService(Id, Description, Hours, PricePerHour, Amount);
    }
}
