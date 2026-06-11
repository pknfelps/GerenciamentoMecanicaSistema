using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.CustomAttributes
{
    internal class GuidValidationAttribute : ValidationAttribute
    {
        public GuidValidationAttribute() 
        {
            ErrorMessage = "O campo {0} não pode ser um Guid vazio.";
        }

        public override bool IsValid(object? value)
        {
            if (value is Guid guid)
                return guid != Guid.Empty;

            return false;
        }
    }
}
