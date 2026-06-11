using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Stock
{
    public class PartUpdateDto<T>(Guid id, T value)
    {
        [Required, GuidValidation]
        public Guid Id { get; private set; } = id;
        [Required, GenericValueValidation]
        public T Value { get; private set; } = value;
    }
}
