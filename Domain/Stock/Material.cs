using Domain.Interface.Stock;

namespace Domain.Stock
{
    public class Material : IMaterial
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Brand { get; private set; }
        public decimal Price { get; private set; }
        public int Amount { get; private set; }
        public int ReservedAmount { get; private set; }

        public Material(string name, string brand, decimal price, int amount) : this(Guid.NewGuid(), name, brand, price, amount) { }

        public Material(Guid id, string name, string brand, decimal price, int amount, int reservedAmount = 0)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentException.ThrowIfNullOrEmpty(brand);

            if (price <= 0)
                throw new ArgumentException($"Preço não pode ser menor ou igual a 0");

            if (amount < 0)
                throw new ArgumentException($"Quantidade não pode ser menor ou igual a 0");

            if (reservedAmount < 0)
                throw new ArgumentException($"Quantidade reservada não pode ser menor ou igual a 0");

            Id = id;
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

        public void ConsumeReservedAmount(int amount)
        {
            if (amount > ReservedAmount)
                throw new InvalidOperationException("Não é possível consumir mais do que a quantidade em reserva");

            ReservedAmount -= amount;
        }

        public void UpdatePrice(decimal price)
        {
            if (price <= 0)
                throw new InvalidOperationException("Preço não pode ser menor ou igual a 0");

            Price = price;
        }
    }
}
