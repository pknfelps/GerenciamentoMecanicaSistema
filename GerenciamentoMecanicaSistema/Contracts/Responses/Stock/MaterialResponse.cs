using Service.Interface.Dto.CustomAttributes;
using Service.Interface.Results.Stock;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Stock
{
    public class MaterialResponse(Guid id, string name, string brand, double price, int amount, int reservedAmount)
    {
        [Description("Id único do item")]
        [Required, GuidValidation]
        public Guid Id { get; set; } = id;

        [Description("Nome do item")]
        [Required, RegularNonEmptyStringExpression]
        public string Name { get; set; } = name;

        [Description("Marca do item")]
        [Required, RegularNonEmptyStringExpression]
        public string Brand { get; set; } = brand;

        [Description("Preço do item")]
        [Required, GenericValueValidation]
        public double Price { get; set; } = price;

        [Description("Quantidade do item")]
        [Required, GenericValueValidation]
        public int Amount { get; set; } = amount;

        [Description("Quantidade reservada do item")]
        [Required, Range(0, int.MaxValue, ErrorMessage = "O campo {0} não pode ser inferior a 0")]
        public int ReservedAmount { get; set; } = reservedAmount;

        public static MaterialResponse Create(MaterialResult material) => new(material.Id, material.Name, material.Brand, material.Price, material.Amount, material.ReservedAmount);
    }
}
