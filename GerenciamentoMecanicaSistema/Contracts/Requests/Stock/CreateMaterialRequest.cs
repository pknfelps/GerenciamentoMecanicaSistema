using Service.Interface.Commands.Stock;
using GerenciamentoMecanicaSistema.Contracts.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Stock
{
    public class CreateMaterialRequest(string name, string brand, decimal price, int amount)
    {
        [Description("Nome do item")]
        [Required, RegularNonEmptyStringExpression]
        public string Name { get; set; } = name;

        [Description("Marca do item")]
        [Required, RegularNonEmptyStringExpression]
        public string Brand { get; set; } = brand;

        [Description("Preço do item")]
        [Required, GenericValueValidation]
        public decimal Price { get; set; } = price;

        [Description("Quantidade do item")]
        [Required, GenericValueValidation]
        public int Amount { get; set; } = amount;

        public CreateMaterialCommand ToCommand() => new(Name, Brand, Price, Amount);
    }
}
