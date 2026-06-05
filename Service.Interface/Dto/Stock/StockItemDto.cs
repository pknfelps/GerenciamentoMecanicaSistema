using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Stock
{
    public class StockItemDto(string name, string brand, double price, int amount, int reservedAmount)
    {
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Name { get; private set; } = name;
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Brand { get; private set; } = brand;
        [Required]
        public double Price { get; private set; } = price;
        [Required]
        public int Amount { get; private set; } = amount;
        public int ReservedAmount { get; private set; } = reservedAmount;

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var item = (StockItemDto)obj;

            return item.Name == Name && item.Brand == Brand && item.Price == Price && item.Amount == Amount && item.ReservedAmount == ReservedAmount;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Brand, Price, Amount, ReservedAmount);
        }
    }
}
