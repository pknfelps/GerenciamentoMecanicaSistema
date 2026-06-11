using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.CustomAttributes
{
    internal class DateTimeValidationAttribute : ValidationAttribute
    {
        public DateTimeValidationAttribute() 
        {
            ErrorMessage = "O campo {0} não é uma data válida";
        }

        public override bool IsValid(object? value)
        {
            if (value is DateTime date)
                return date != DateTime.MinValue;

            return false;
        }
    }
}
