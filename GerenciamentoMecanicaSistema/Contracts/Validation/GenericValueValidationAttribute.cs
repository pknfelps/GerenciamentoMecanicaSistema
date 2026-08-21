using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Validation
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class GenericValueValidationAttribute : ValidationAttribute
    {
        public GenericValueValidationAttribute()
        {
            ErrorMessage = "O campo {0} deve ser maior que zero para valores inteiros ou maior que 0,1 para valores decimais";
        }

        public override bool IsValid(object? value)
        {
            if (value is int intValue)
                return intValue > 0;

            if (value is float floatValue)
                return floatValue > 0.1;

            if (value is decimal decimalValue)
                return decimalValue > 0.1m;

            return false;
        }
    }
}
