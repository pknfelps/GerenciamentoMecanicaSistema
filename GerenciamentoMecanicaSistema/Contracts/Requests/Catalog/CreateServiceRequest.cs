using Service.Interface.Commands.Catalog;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Catalog
{
    public class CreateServiceRequest(string description, float hours, double pricePerHour, int amount)
    {
        [Description("Descrição do serviço")]
        [Required, RegularNonEmptyStringExpression]
        public string Description { get; set; } = description;

        [Description("Tempo para conclusão do serviço")]
        [Required, GenericValueValidation]
        public float Hours { get; set; } = hours;

        [Description("Preço por hora do serviço")]
        [Required, GenericValueValidation]
        public double PricePerHour { get; set; } = pricePerHour;

        [Description("Quantidade de serviços")]
        [Required, GenericValueValidation]
        public int Amount { get; set; } = amount;

        public CreateServiceCommand ToCommand() => new(Description, Hours, PricePerHour, Amount);
    }
}
