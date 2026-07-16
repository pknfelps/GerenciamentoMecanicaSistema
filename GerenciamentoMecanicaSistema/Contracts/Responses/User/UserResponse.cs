using Service.Interface.Results.User;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.User
{
    public class UserResponse(Guid id, string name, string password, string role)
    {
        public Guid Id { get; set; } = id;
        public string Name { get; set; } = name;
        public string Password { get; set; } = password;
        public string Role { get; set; } = role;

        public static UserResponse Create(UserResult user) => new(user.Id, user.Name, user.Password, user.Role);
    }
}
