using Service.Interface.Results.Catalog;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Catalog
{
    public class ServiceResponse(Guid id, string description, float hours, decimal pricePerHour, int amount)
    {
        public Guid Id { get; set; } = id;
        public string Description { get; set; } = description;
        public float Hours { get; set; } = hours;
        public decimal PricePerHour { get; set; } = pricePerHour;
        public int Amount { get; set; } = amount;

        public static ServiceResponse Create(ServiceResult service) => new(service.Id, service.Description, service.Hours, service.PricePerHour, service.Amount);
    }
}
