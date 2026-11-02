using System.Security.Claims;

namespace GerenciamentoMecanicaSistema.Authorization
{
    public static class AuthenticatedIdentity
    {
        public static bool TryGetId(ClaimsPrincipal principal, out Guid id)
        {
            id = Guid.Empty;
            var identifiers = principal.FindAll(ClaimTypes.NameIdentifier).ToArray();
            return identifiers.Length == 1
                && Guid.TryParse(identifiers[0].Value, out id)
                && id != Guid.Empty;
        }
    }
}
