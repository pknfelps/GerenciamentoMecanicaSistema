using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Order
{
    public class ApproveOrderDto(Guid orderId, bool approved)
    {
        [GuidValidation]
        public Guid OrderId { get; private set; } = orderId;
        [Required]
        public bool Approved { get; private set; } = approved;

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            var approve = (ApproveOrderDto)obj;

            return OrderId == approve.OrderId && Approved == approve.Approved;
        }
    }
}
