using Domain.Interface.Stock;

namespace Domain.Stock
{
    public class StockItem : IStockItem
    {
        public string Name { get; private set; }
        public string Brand { get; private set; }
        public double Price { get; private set; }
        public int Amount { get; private set; }
        public int ReservedAmount { get; private set; }

        public StockItem(string name, string brand, double price, int amount, int reservedAmount = 0)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentException.ThrowIfNullOrEmpty(brand);

            if (price <= 0)
                throw new ArgumentException($"Preço não pode ser menor ou igual a 0");

            if (amount < 0)
                throw new ArgumentException($"Quantidade não pode ser menor ou igual a 0");

            if (reservedAmount < 0)
                throw new ArgumentException($"Quantidade reservada não pode ser menor ou igual a 0");

            Name = name;
            Brand = brand;
            Price = price;
            Amount = amount;
            ReservedAmount = reservedAmount;
        }

        public void AddAmount(int amount) => Amount += amount;

        public void RemoveAmount(int amount)
        {
            if (amount > Amount)
                throw new InvalidOperationException("Não é possível remover mais do que a quantidade em estoque");

            Amount -= amount;
        }

        public void ReserveAmount(int amount)
        {
            if (amount > Amount)
                throw new InvalidOperationException("Não é possível reservar mais do que a quantidade em estoque");

            Amount -= amount;
            ReservedAmount += amount;
        }

        public void RestoreAmount(int amount)
        {
            if (amount > ReservedAmount)
                throw new InvalidOperationException("Não é possível restaurar mais do que a quantidade em reserva");

            ReservedAmount -= amount;
            Amount += amount;
        }

        public void UpdatePrice(double price)
        {
            if (price <= 0)
                throw new InvalidOperationException("Preço não pode ser menor ou igual a 0");

            Price = price;
        }
    }
}
