using Domain.Interface.User;
using Domain.User;
using System.Text.Json.Serialization;

namespace Infrastructure.Persistence.PostgreSql.Models
{
    internal class UserDb
    {
        [JsonPropertyName("id")]
        public Guid Id { get; private set; } = Guid.Empty;
        [JsonPropertyName("name")]
        public string Name { get; private set; } = "";
        [JsonPropertyName("password")]
        public string Password { get; private set; } = "";
        [JsonPropertyName("role")]
        public string Role { get; private set; } = "";

        public static UserDb Create(IUserCredentials credentials) => new()
        {
            Id = credentials.User.Id,
            Name = credentials.User.Name,
            Password = credentials.PasswordHash,
            Role = credentials.User.Role.ToString()
        };

        public IUser ToDomain() => new User(Id, Name, Role);

        public IUserCredentials ToCredentials() => new UserCredentials(ToDomain(), Password);
    }
}
