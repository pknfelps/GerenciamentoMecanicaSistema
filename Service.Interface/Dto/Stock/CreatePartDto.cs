using Domain.Interface.Stock;
using Domain.Stock;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Stock
{
    public class CreatePartDto(string name, string brand, double price, int amount)
    {
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Name { get; private set; } = name;
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Brand { get; private set; } = brand;
        [Required, Range(0.1, double.MaxValue, ErrorMessage = "O campoo {0} não pode ser abaixo de 0.1")]
        public double Price { get; private set; } = price;
        [Required, Range(1, int.MaxValue, ErrorMessage = "O campo {0} não pode ser inferior a 1")]
        public int Amount { get; private set; } = amount;

        public virtual IPart ToDomain() => new Part(Name, Brand, Price, Amount);

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            var part = (CreatePartDto)obj;

            return Name == part.Name && Brand == part.Brand && Price == part.Price && Amount == part.Amount;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Brand, Price, Amount);
        }
    }
}
