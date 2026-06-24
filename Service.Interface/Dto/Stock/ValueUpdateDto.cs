using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Stock
{
    public class ValueUpdateDto<T>(T value)
    {
        [Description("Quantidade a atualizar do item")]
        [Required, GenericValueValidation]
        public T Value { get; set; } = value;
    }
}
