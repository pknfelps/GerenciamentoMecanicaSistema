using Domain.Interface.User;
using Domain.User;
using System.Text.Json.Serialization;

namespace Repository.PersistenceModels
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

        public static UserDb Create(IUser user) => new() { Id = user.Id, Name = user.Name, Password = user.Password.Secret, Role = user.Role.ToString() };

        public IUser ToDomain() => new User(Id, Name, Password, Role);
    }
}
