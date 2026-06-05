namespace Repository.Dto
{
    internal class StockItemDb(string item_name, string brand, double price, int amount, int reserved_amount)
    {
        public string Name { get; private set; } = item_name;
        public string Brand { get; private set; } = brand;
        public double Price { get; private set; } = price;
        public int Amount { get; private set; } = amount;
        public int ReservedAmount { get; private set; } = reserved_amount;
    }
}
