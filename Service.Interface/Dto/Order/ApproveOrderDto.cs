using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Order
{
    public class ApproveOrderDto(string customerDocument, bool approved)
    {
        [Description("Número de documento do cliente")]
        [Required, RegularDocumentExpression]
        public string CustomerDocument { get; set; } = customerDocument;
        [Description("Se o orçamento foi aprovado")]
        [Required]
        public bool Approved { get; set; } = approved;

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            var approve = (ApproveOrderDto)obj;

            return CustomerDocument == approve.CustomerDocument && Approved == approve.Approved;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CustomerDocument, Approved);
        }
    }
}
