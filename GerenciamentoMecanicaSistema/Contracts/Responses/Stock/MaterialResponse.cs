using Service.Interface.Results.Stock;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Stock
{
    public class MaterialResponse(Guid id, string name, string brand, decimal price, int amount, int reservedAmount)
    {
        public Guid Id { get; set; } = id;
        public string Name { get; set; } = name;
        public string Brand { get; set; } = brand;
        public decimal Price { get; set; } = price;
        public int Amount { get; set; } = amount;
        public int ReservedAmount { get; set; } = reservedAmount;

        public static MaterialResponse Create(MaterialResult material) => new(material.Id, material.Name, material.Brand, material.Price, material.Amount, material.ReservedAmount);
    }
}
