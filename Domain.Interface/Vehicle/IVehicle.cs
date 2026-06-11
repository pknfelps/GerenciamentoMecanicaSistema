using Domain.Interface.Custumer;

namespace Domain.Interface.Vehicle
{
    public interface IVehicle : IEntity
    {
        IDocument CustomerDocument { get; }
        string Brand { get; }
        string Model { get; }
        int Year { get; }
        ILicensePlate LicensePlate { get; }
    }
}
