using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Service.Interface.Dto.CustomAttributes
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
