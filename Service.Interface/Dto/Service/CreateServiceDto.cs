using Domain.Interface.Service;
using Domain.MechanicalService;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Service
{
    public class CreateServiceDto(string description, float hours, double pricePerHour)
    {
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Description { get; private set; } = description;
        [Required, Range(0, float.MaxValue, ErrorMessage = "Valor deve ser positivo.")]
        public float Hours { get; private set; } = hours;
        [Required, Range(0, double.MaxValue, ErrorMessage = "Valor deve ser positivo.")]
        public double PricePerHour { get; private set; } = pricePerHour;

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
