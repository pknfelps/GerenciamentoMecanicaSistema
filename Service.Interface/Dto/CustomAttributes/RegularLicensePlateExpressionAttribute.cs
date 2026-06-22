using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class RegularLicensePlateExpressionAttribute : RegularExpressionAttribute
    {
        public RegularLicensePlateExpressionAttribute() : base(@"^[a-zA-Z0-9]{7}$")
        {
            ErrorMessage = "O campo {0} não é uma placa válida";
        }
    }
}
