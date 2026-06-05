namespace Domain.Interface.Vehicle
{
    public interface IVehicle : IEntity
    {
        string Brand { get; }
        string Model { get; }
        int Year { get; }
        ILicensePlate LicensePlate { get; }
    }
}
