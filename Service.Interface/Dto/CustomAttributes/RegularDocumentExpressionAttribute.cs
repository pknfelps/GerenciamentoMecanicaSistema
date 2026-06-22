using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class RegularDocumentExpressionAttribute : RegularExpressionAttribute
    {
        public RegularDocumentExpressionAttribute() : base(@"^(\d{3}\.\d{3}\.\d{3}-\d{2}|\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}|\d{11}|\d{14})$")
        {
            ErrorMessage = "O campo {0} não é um documento válido";
        }
    }
}
