namespace Service.Interface.Commands.Catalog
{
    public record CreateServiceCommand(string Description, float Hours, double PricePerHour, int Amount);
}
