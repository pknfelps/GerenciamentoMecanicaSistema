using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto
{
    public class UpdateItemDto<T>(Guid id, T value)
    {
        [Description("Id único do item")]
        [Required, GuidValidation]
        public Guid Id { get; set; } = id;
        [Description("Quantidade a atualizar do item")]
        [Required, GenericValueValidation]
        public T Value { get; set; } = value;
    }
}
