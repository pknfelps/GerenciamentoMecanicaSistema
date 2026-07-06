using Service.Interface.Dto.CustomAttributes;
using Service.Interface.Results.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.User
{
    public class UserResponse(Guid id, string name, string password, string role)
    {
        [Description("Id único do usuário")]
        [Required, GuidValidation]
        public Guid Id { get; set; } = id;

        [Description("Nome do usuário")]
        [Required, RegularNonEmptyStringExpression]
        public string Name { get; set; } = name;

        [Description("Senha do usuário")]
        [Required]
        public string Password { get; set; } = password;

        [Description("Cargo do usuário")]
        [Required, RegularNonEmptyStringExpression]
        public string Role { get; set; } = role;

        public static UserResponse Create(UserResult user) => new(user.Id, user.Name, user.Password, user.Role);
    }
}
