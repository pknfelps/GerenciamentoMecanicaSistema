using System.ComponentModel;
using GerenciamentoMecanicaSistema.Contracts.Validation;
using Service.Interface.Commands.Order;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Order
{
    public class CreateOrderRequest(
        Guid customerId,
        Guid vehicleId,
        IReadOnlyCollection<UpdateOrderItemRequest<int>>? services = null,
        IReadOnlyCollection<UpdateOrderItemRequest<int>>? materials = null) : IValidatableObject
    {
        [Description("Identificação do cliente")]
        [Required, GuidValidation]
        public Guid CustomerId { get; set; } = customerId;

        [Description("Identificação do veículo")]
        [Required, GuidValidation]
        public Guid VehicleId { get; set; } = vehicleId;

        [Description("Lista de serviços solicitados")]
        public IReadOnlyCollection<UpdateOrderItemRequest<int>>? Services { get; set; } = services;

        [Description("Lista de materiais necessários para o serviço")]
        public IReadOnlyCollection<UpdateOrderItemRequest<int>>? Materials { get; set; } = materials;

        public CreateOrderCommand ToCommand() => new(
            CustomerId,
            VehicleId,
            [.. Services?.Select(service => service.ToCommand()) ?? []],
            [.. Materials?.Select(material => material.ToCommand()) ?? []]);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Services?.Any(item => item is null) == true)
                yield return new ValidationResult("A lista de serviços não pode conter itens nulos.", [nameof(Services)]);
            else if (Services?.GroupBy(item => item.Id).Any(group => group.Count() > 1) == true)
                yield return new ValidationResult("A lista de serviços não pode conter identificadores duplicados.", [nameof(Services)]);

            if (Materials?.Any(item => item is null) == true)
                yield return new ValidationResult("A lista de materiais não pode conter itens nulos.", [nameof(Materials)]);
            else if (Materials?.GroupBy(item => item.Id).Any(group => group.Count() > 1) == true)
                yield return new ValidationResult("A lista de materiais não pode conter identificadores duplicados.", [nameof(Materials)]);
        }
    }
}
