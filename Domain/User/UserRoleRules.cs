using Domain.Interface.Exceptions;
using Domain.Interface.User;

namespace Domain.User
{
    public static class UserRoleRules
    {
        public static bool IsValid(string? role) => role is nameof(Roles.Admin) or nameof(Roles.Mechanic);

        public static Roles Parse(string? role) => role switch
        {
            nameof(Roles.Admin) => Roles.Admin,
            nameof(Roles.Mechanic) => Roles.Mechanic,
            _ => throw new DomainValidationException("Cargo inválido: informe Admin ou Mechanic")
        };
    }
}
