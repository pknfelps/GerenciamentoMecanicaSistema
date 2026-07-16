using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Validation
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class RegularNonEmptyStringExpressionAttribute : RegularExpressionAttribute
    {
        public RegularNonEmptyStringExpressionAttribute() : base(@"^[a-zA-ZÀ-ÿ\s]{3,}$")
        {
            ErrorMessage = "O campo {0} deve conter ao menos 3 caracteres";
        }
    }
}
