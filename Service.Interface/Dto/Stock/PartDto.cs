using Domain.Interface.Stock;
using Domain.Stock;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Stock
{
    public class PartDto(Guid id, string name, string brand, double price, int amount, int reservedAmount) : CreatePartDto(name, brand, price, amount)
    {
        [Description("Id único do item")]
        [Required, GuidValidation]
        public Guid Id { get; set; } = id;
        [Description("Quantidade reservada do item")]
        [Required, Range(0, int.MaxValue, ErrorMessage = "O campo {0} não pode ser inferior a 0")]
        public int ReservedAmount { get; set; } = reservedAmount;

        public static PartDto Create(IPart part) => new(part.Id, part.Name, part.Brand, part.Price, part.Amount, part.ReservedAmount);

        public override IPart ToDomain() => new Part(Id, Name, Brand, Price, Amount, ReservedAmount);

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var item = (PartDto)obj;

            return item.Name == Name && item.Brand == Brand && item.Price == Price && item.Amount == Amount && item.ReservedAmount == ReservedAmount;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Brand, Price, Amount, ReservedAmount);
        }
    }
}
