using Service.Interface.Commands.Order;
using GerenciamentoMecanicaSistema.Contracts.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Order
{
    public class UpdateOrderItemRequest<T>(Guid id, T value)
    {
        [Description("Id único do item")]
        [Required, GuidValidation]
        public Guid Id { get; set; } = id;

        [Description("Quantidade a atualizar do item")]
        [Required, GenericValueValidation]
        public T Value { get; set; } = value;

        public UpdateOrderItemCommand<T> ToCommand() => new(Id, Value);
    }
}
