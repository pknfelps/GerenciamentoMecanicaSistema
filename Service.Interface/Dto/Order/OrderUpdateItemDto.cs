using Domain.Interface.Stock;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Order
{
    public class OrderUpdateItemDto(Guid orderId, Guid itemId, int amount)
    {
        [Required, GuidValidation]
        public Guid OrderId { get; private set; } = orderId;
        [Required, GuidValidation]
        public Guid ItemId { get; private set; } = itemId;
        [Required, Range(1, int.MaxValue, ErrorMessage = "O campo {0} não pode ser inferior a 1")]
        public int Amount { get; private set; } = amount;

        public static OrderUpdateItemDto Create(Guid orderId, IPart item) => new(orderId, item.Id, item.Amount);
    }
}
