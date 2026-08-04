using GerenciamentoMecanicaSistema.Contracts.Requests.Customer;
using GerenciamentoMecanicaSistema.Contracts.Requests.Vehicle;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Order
{
    public class CreateOrderRequest(
        CreateCustomerRequest customer,
        CreateVehicleRequest vehicle,
        IReadOnlyCollection<UpdateOrderItemRequest<int>> services,
        IReadOnlyCollection<UpdateOrderItemRequest<int>> materials) : IValidatableObject
    {
        [Required]
        public CreateCustomerRequest Customer { get; set; } = customer;

        [Required]
        public CreateVehicleRequest Vehicle { get; set; } = vehicle;

        [Required]
        public IReadOnlyCollection<UpdateOrderItemRequest<int>> Services { get; set; } = services;

        [Required]
        public IReadOnlyCollection<UpdateOrderItemRequest<int>> Materials { get; set; } = materials;

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
