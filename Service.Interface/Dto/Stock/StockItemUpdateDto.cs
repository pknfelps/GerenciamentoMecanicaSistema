using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Stock
{
    public class StockItemUpdateDto<T>(string name, string brand, T value)
    {
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Name { get; private set; } = name;
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Brand { get; private set; } = brand;
        [Required, Range(0, double.MaxValue, ErrorMessage = "Valor deve ser positivo.")]
        public T Value { get; private set; } = value;
    }
}
