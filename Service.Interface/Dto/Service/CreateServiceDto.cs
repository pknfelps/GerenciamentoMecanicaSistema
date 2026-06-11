using Domain.Interface.Service;
using Domain.MechanicalService;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Service
{
    public class CreateServiceDto(string description, float hours, double pricePerHour, int amount)
    {
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Description { get; private set; } = description;
        [Required, Range(0, float.MaxValue, ErrorMessage = "Valor deve ser positivo.")]
        public float Hours { get; private set; } = hours;
        [Required, Range(0, double.MaxValue, ErrorMessage = "Valor deve ser positivo.")]
        public double PricePerHour { get; private set; } = pricePerHour;
        [Required, Range(1, int.MaxValue, ErrorMessage = "Quantidade deve maior do que 0.")]
        public int Amount { get; private set; } = amount;

        public virtual IMechanicalService ToDomain() => new MechanicalService(Description, Hours, PricePerHour, Amount);
    }
}
