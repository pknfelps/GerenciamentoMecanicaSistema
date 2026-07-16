using Service.Interface.Results.Vehicle;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Vehicle
{
    public class VehicleResponse(Guid id, string customerDocument, string brand, string model, int year, string licensePlate)
    {
        public Guid Id { get; set; } = id;
        public string CustomerDocument { get; set; } = customerDocument;
        public string Brand { get; set; } = brand;
        public string Model { get; set; } = model;
        public int Year { get; set; } = year;
        public string LicensePlate { get; set; } = licensePlate;

        public static VehicleResponse Create(VehicleResult vehicle) => new(vehicle.Id, vehicle.CustomerDocument, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.LicensePlate);
    }
}
