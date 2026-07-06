using Domain.Interface.Stock;

namespace Service.Interface.Results.Stock
{
    public record MaterialResult(Guid Id, string Name, string Brand, double Price, int Amount, int ReservedAmount)
    {
        public static MaterialResult Create(IMaterial material) => new(material.Id, material.Name, material.Brand, material.Price, material.Amount, material.ReservedAmount);
    }
}
