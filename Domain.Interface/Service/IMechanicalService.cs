namespace Domain.Interface.Service
{
    public interface IMechanicalService : IEntity
    {
        string Description { get; }
        float Hours { get; }
        double PricePerHour { get; }
        double Price { get; }
        int Amount { get; }

        void UpdateDescriptrion(string newDescription);
        void UpdateHours(float newHours);
        void UpdatePricePerHour(double newPricePerHour);
        void AddServiceAmount(int amount);
        void RemoveServiceAmount(int amount);
    }
}
