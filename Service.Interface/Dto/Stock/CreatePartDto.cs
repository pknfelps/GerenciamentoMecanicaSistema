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
    }
}
