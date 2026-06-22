using Domain.Interface.Service;
using Domain.MechanicalService;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Service
{
    public class CreateServiceDto(string description, float hours, double pricePerHour, int amount)
    {
        [Description("Descrição do serviço")]
        [Required, RegularNonEmptyStringExpression]
        public string Description { get; set; } = description;
        [Description("Tempo para conclusão do serviço")]
        [Required, GenericValueValidation]
        public float Hours { get; set; } = hours;
        [Description("Preço por hora do serviço")]
        [Required, GenericValueValidation]
        public double PricePerHour { get; set; } = pricePerHour;
        [Description("Quantidade de serviços")]
        [Required, GenericValueValidation]
        public int Amount { get; set; } = amount;

        public virtual IMechanicalService ToDomain() => new MechanicalService(Description, Hours, PricePerHour);

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            var service = (CreateServiceDto)obj;

            return Description == service.Description && Hours == service.Hours && PricePerHour == service.PricePerHour;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Description, Hours, PricePerHour);
        }
    }
}
