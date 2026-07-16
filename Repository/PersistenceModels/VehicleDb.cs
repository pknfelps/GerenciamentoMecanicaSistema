using Domain.Interface.Vehicle;
using Domain.Vehicle;
using System.Text.Json.Serialization;

namespace Repository.PersistenceModels
{
    internal class VehicleDb
    {
        [JsonPropertyName("id")]
        public Guid Id { get; private set; } = Guid.Empty;
        [JsonPropertyName("customer_document")]
        public string CustomerDocument { get; private set; } = "";
        [JsonPropertyName("brand")]
        public string Brand { get; private set; } = "";
        [JsonPropertyName("model")]
        public string Model { get; private set; } = "";
        [JsonPropertyName("year")]
        public int Year { get; private set; } = 0;
        [JsonPropertyName("license_plate")]
        public string LicensePlate { get; private set; } = "";

        public static VehicleDb Create(IVehicle vehicle) => new() { Id = vehicle.Id, CustomerDocument = vehicle.CustomerDocument.Id, Brand = vehicle.Brand, Model = vehicle.Model, Year = vehicle.Year, LicensePlate = vehicle.LicensePlate.License };

        public IVehicle ToDomain() => new Vehicle(Id, CustomerDocument, Brand, Model, Year, LicensePlate);
    }
}
