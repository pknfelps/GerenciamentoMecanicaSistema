using Domain.User;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Validation
{
    public sealed class UserRoleValidationAttribute : ValidationAttribute
    {
        public UserRoleValidationAttribute() : base("Cargo inválido: informe Admin ou Mechanic") { }

        public override bool IsValid(object? value) => value is string role && UserRoleRules.IsValid(role);
    }
}
