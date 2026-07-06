using Service.Interface.Dto.CustomAttributes;
using Service.Interface.Results.Catalog;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Catalog
{
    public class ServiceResponse(Guid id, string description, float hours, double pricePerHour, int amount)
    {
        [Description("Id único do serviço")]
        [Required, GuidValidation]
        public Guid Id { get; set; } = id;

        public string Description { get; set; } = description;
        public float Hours { get; set; } = hours;
        public double PricePerHour { get; set; } = pricePerHour;
        public int Amount { get; set; } = amount;

        public static ServiceResponse Create(ServiceResult service) => new(service.Id, service.Description, service.Hours, service.PricePerHour, service.Amount);
    }
}
