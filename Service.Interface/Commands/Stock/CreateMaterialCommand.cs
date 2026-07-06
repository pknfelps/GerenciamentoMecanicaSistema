namespace Service.Interface.Commands.Stock
{
    public record CreateMaterialCommand(string Name, string Brand, double Price, int Amount);
}
