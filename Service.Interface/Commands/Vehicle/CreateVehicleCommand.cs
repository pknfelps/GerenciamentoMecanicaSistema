namespace Service.Interface.Commands.Vehicle
{
    public record CreateVehicleCommand(string CustomerDocument, string Brand, string Model, int Year, string LicensePlate);
}
