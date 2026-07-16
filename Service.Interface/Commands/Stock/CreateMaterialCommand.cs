namespace Service.Interface.Commands.Stock
{
    public record CreateMaterialCommand(string Name, string Brand, decimal Price, int Amount);
}
