namespace Service.Interface.Results.Catalog
{
    public record ServiceResult(Guid Id, string Description, float Hours, decimal PricePerHour, int Amount);
}
