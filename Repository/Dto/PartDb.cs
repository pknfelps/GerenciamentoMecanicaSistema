using Domain.Interface.Stock;
using Domain.Stock;
using System.Text.Json.Serialization;

namespace Repository.Dto
{
    internal class PartDb
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }
        [JsonPropertyName("name")]
        public string Name { get; init; }
        [JsonPropertyName("brand")]
        public string Brand { get; init; }
        [JsonPropertyName("price")]
        public double Price { get; init; }
        [JsonPropertyName("amount")]
        public int Amount { get; init; }
        [JsonPropertyName("reserved_amount")]
        public int Reserved_Amount { get; init; }

        public static PartDb Create(IPart part) => new() { Id = part.Id, Name = part.Name, Brand = part.Brand, Price = part.Price, Amount = part.Amount, Reserved_Amount = part.ReservedAmount };

        public IPart ToDomain() => new Part(Id, Name, Brand, Price, Amount, Reserved_Amount);
    }
}
