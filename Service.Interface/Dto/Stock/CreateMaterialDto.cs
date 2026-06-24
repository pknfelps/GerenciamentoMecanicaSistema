using Domain.Interface.Stock;
using Domain.Stock;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Stock
{
    public class CreateMaterialDto(string name, string brand, double price, int amount)
    {
        [Description("Nome do item")]
        [Required, RegularNonEmptyStringExpression]
        public string Name { get; set; } = name;
        [Description("Marca do item")]
        [Required, RegularNonEmptyStringExpression]
        public string Brand { get; set; } = brand;
        [Description("Preço do item")]
        [Required, GenericValueValidation]
        public double Price { get; set; } = price;
        [Description("Quantidade do item")]
        [Required, GenericValueValidation]
        public int Amount { get; set; } = amount;

        public virtual IMaterial ToDomain() => new Material(Name, Brand, Price, Amount);

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            var part = (CreateMaterialDto)obj;

            return Name == part.Name && Brand == part.Brand && Price == part.Price && Amount == part.Amount;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Brand, Price, Amount);
        }
    }
}
