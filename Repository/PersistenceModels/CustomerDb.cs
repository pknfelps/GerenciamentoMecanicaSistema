using Domain.Customer;
using Domain.Interface.Custumer;
using System.Text.Json.Serialization;

namespace Repository.PersistenceModels
{
    internal class CustomerDb
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; } = Guid.Empty;
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";
        [JsonPropertyName("document")]
        public string Document { get; init; } = "";
        [JsonPropertyName("phone")]
        public string Phone { get; init; } = "";
        [JsonPropertyName("email")]
        public string Email { get; init; } = "";

        public static CustomerDb Create(ICustomer customer) => new() { Id = customer.Id, Name = customer.Name, Document = customer.Document.Id, Phone = customer.Phone.Number, Email = customer.Email.Address };

        public ICustomer ToDomain() => new Customer(Id, Name, Document, Phone, Email);
    }
}
