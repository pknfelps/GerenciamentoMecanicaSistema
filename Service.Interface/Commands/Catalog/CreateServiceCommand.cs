namespace Service.Interface.Commands.Catalog
{
    public record CreateServiceCommand(string Description, float Hours, decimal PricePerHour, int Amount);
}
