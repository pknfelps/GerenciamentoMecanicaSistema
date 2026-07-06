using Service.Interface.Commands.User;
using GerenciamentoMecanicaSistema.Contracts.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.User
{
    public class CreateUserRequest(string name, string password, string role)
    {
        [Description("Nome do usuário")]
        [Required, RegularNonEmptyStringExpression]
        public string Name { get; set; } = name;

        [Description("Senha do usuário")]
        [Required]
        public string Password { get; set; } = password;

        [Description("Cargo do usuário")]
        [Required, RegularNonEmptyStringExpression]
        public string Role { get; set; } = role;

        public CreateUserCommand ToCommand() => new(Name, Password, Role);
    }
}
