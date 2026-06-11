using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.CustomAttributes
{
    internal class RegularLicensePlateExpression : RegularExpressionAttribute
    {
        public RegularLicensePlateExpression() : base(@"^[a-zA-Z0-9]+$")
        {
            ErrorMessage = "O campo {0} não é uma placa válida";
        }
    }
}
