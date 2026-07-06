using Service.Interface.Commands.Order;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Order
{
    public class ApproveOrderRequest(string customerDocument, bool approved)
    {
        [Description("Número de documento do cliente")]
        [Required, RegularDocumentExpression]
        public string CustomerDocument { get; set; } = customerDocument;

        [Description("Se o orçamento foi aprovado")]
        [Required]
        public bool Approved { get; set; } = approved;

        public ApproveOrderCommand ToCommand() => new(CustomerDocument, Approved);
    }
}
