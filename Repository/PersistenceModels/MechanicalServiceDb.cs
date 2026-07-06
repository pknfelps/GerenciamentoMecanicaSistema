using Domain.Interface.Service;
using Domain.MechanicalService;
using System.Text.Json.Serialization;

namespace Repository.PersistenceModels
{
    internal class MechanicalServiceDb
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; } = Guid.Empty;
        [JsonPropertyName("description")]
        public string Description { get; init; } = "";
        [JsonPropertyName("hours")]
        public float Hours { get; init; } = 0f;
        [JsonPropertyName("price_per_hour")]
        public double PricePerHour { get; init; } = 0.0;
        [JsonPropertyName("amount")]
        public int Amount { get; init; } = 0;

        public static MechanicalServiceDb Create(IMechanicalService service) => new() { Id = service.Id, Description = service.Description, Hours = service.Hours, PricePerHour = service.PricePerHour, Amount = service.Amount };

        public IMechanicalService ToDomain() => new MechanicalService(Id, Description, Hours, PricePerHour, Amount == 0 ? 1 : Amount);
    }
}
