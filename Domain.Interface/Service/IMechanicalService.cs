namespace Domain.Interface.Service
{
    public interface IMechanicalService : IEntity
    {
        string Description { get; }
        float Hours { get; }
        decimal PricePerHour { get; }
        decimal Price { get; }
        int Amount { get; }

        void UpdateDescriptrion(string newDescription);
        void UpdateHours(float newHours);
        void UpdatePricePerHour(decimal newPricePerHour);
        void AddServiceAmount(int amount);
        void RemoveServiceAmount(int amount);
    }
}
