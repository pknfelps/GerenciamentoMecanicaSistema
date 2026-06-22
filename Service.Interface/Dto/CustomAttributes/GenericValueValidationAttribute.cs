using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    internal class GenericValueValidationAttribute : ValidationAttribute
    {
        public GenericValueValidationAttribute()
        {
            ErrorMessage = "O campo {0} deve ser maior que 0 ou 0.1";
        }

        public override bool IsValid(object? value)
        {
            if (value is int intValue)
                return intValue > 0;

            if (value is float floatValue)
                return floatValue > 0.1;

            if (value is double doubleValue)
                return doubleValue > 0.1;

            return false;
        }
    }
}
