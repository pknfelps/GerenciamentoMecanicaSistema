namespace Service.Interface.Results.Catalog
{
    public record ServiceResult(Guid Id, string Description, float Hours, double PricePerHour, int Amount);
}
