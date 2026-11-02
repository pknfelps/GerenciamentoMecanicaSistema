using Domain.Customer;
using Domain.Interface.Custumer;
using System.Text.Json.Serialization;
using System.Data;

namespace Infrastructure.Persistence.PostgreSql.Models
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
        [JsonPropertyName("status")]
        public string Status { get; init; } = "";

        public static CustomerDb Create(ICustomer customer) => new() { Id = customer.Id, Name = customer.Name, Document = customer.Document.Id, Phone = customer.Phone.Number, Email = customer.Email.Address, Status = customer.Status.ToString() };

        public ICustomer ToDomain() => new Customer(Id, Name, Document, Phone, Email, Status switch
            {
                nameof(CustomerStatus.Active) => CustomerStatus.Active,
                nameof(CustomerStatus.Inactive) => CustomerStatus.Inactive,
                _ => throw new DataException("Status persistido do cliente é inválido")
            });
    }
}
