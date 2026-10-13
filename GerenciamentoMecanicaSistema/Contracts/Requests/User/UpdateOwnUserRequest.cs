using GerenciamentoMecanicaSistema.Contracts.Validation;
using Service.Interface.Commands.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.User
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class UpdateOwnUserRequest : IValidatableObject
    {
        private string? name;
        private string? password;
        private bool nameProvided;
        private bool passwordProvided;

        [Description("Novo nome do usuário")]
        [RegularNonEmptyStringExpression, MaxLength(100)]
        public string? Name
        {
            get => name;
            set { name = value; nameProvided = true; }
        }

        [Description("Nova senha do usuário")]
        public string? Password
        {
            get => password;
            set { password = value; passwordProvided = true; }
        }

        public UpdateOwnUserCommand ToCommand() => new(Name, Password);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!nameProvided && !passwordProvided)
                yield return new ValidationResult("Informe nome ou senha para atualizar a conta.");
            if (nameProvided && string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("O nome informado deve ser preenchido.", [nameof(Name)]);
            if (passwordProvided && string.IsNullOrWhiteSpace(Password))
                yield return new ValidationResult("A senha informada deve ser preenchida.", [nameof(Password)]);
        }
    }
}
