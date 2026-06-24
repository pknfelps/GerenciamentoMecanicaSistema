using Domain.Interface.Stock;
using Domain.Stock;
using System.Text.Json.Serialization;

namespace Repository.Dto
{
    internal class MaterialDb
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; } = Guid.Empty;
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";
        [JsonPropertyName("brand")]
        public string Brand { get; init; } = "";
        [JsonPropertyName("price")]
        public double Price { get; init; } = 0.0;
        [JsonPropertyName("amount")]
        public int Amount { get; init; } = 0;
        [JsonPropertyName("reserved_amount")]
        public int Reserved_Amount { get; init; } = 0;

        public static MaterialDb Create(IMaterial material) => new() { Id = material.Id, Name = material.Name, Brand = material.Brand, Price = material.Price, Amount = material.Amount, Reserved_Amount = material.ReservedAmount };

        public IMaterial ToDomain() => new Material(Id, Name, Brand, Price, Amount, Reserved_Amount);
    }
}
