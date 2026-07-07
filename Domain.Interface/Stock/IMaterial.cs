namespace Domain.Interface.Stock
{
    public interface IMaterial : IEntity
    {
        string Name { get; }
        string Brand { get; }
        decimal Price { get; }
        int Amount { get; }
        int ReservedAmount { get; }
        void AddAmount(int amount);
        void RemoveAmount(int amount);
        void ReserveAmount(int amount);
        void RestoreAmount(int amount);
        void ConsumeReservedAmount(int amount);
        void UpdatePrice(decimal price);
    }
}
