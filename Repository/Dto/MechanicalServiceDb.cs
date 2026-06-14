using Domain.Interface.Service;
using Domain.MechanicalService;
using System.Text.Json.Serialization;

namespace Repository.Dto
{
    internal class MechanicalServiceDb
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }
        [JsonPropertyName("description")]
        public string Description { get; init; }
        [JsonPropertyName("hours")]
        public float Hours { get; init; }
        [JsonPropertyName("price_per_hour")]
        public double Price_Per_Hour { get; init; }
        [JsonPropertyName("amount")]
        public int Amount { get; init; }

        public static MechanicalServiceDb Create(IMechanicalService service) => new() { Id = service.Id, Description = service.Description, Hours = service.Hours, Price_Per_Hour = service.PricePerHour, Amount = service.Amount };

        public IMechanicalService ToDomain() => new MechanicalService(Id, Description, Hours, Price_Per_Hour, Amount == 0 ? 1 : Amount);
    }
}
