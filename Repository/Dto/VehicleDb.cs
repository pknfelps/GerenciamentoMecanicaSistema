using Domain.Interface.Vehicle;
using Domain.Vehicle;
using System.Text.Json.Serialization;

namespace Repository.Dto
{
    public class VehicleDb
    {
        [JsonPropertyName("id")]
        public Guid Id { get; private set; }
        [JsonPropertyName("customer_document")]
        public string Customer_Document { get; private set; }
        [JsonPropertyName("brand")]
        public string Brand { get; private set; }
        [JsonPropertyName("model")]
        public string Model { get; private set; }
        [JsonPropertyName("year")]
        public int Year { get; private set; }
        [JsonPropertyName("license_plate")]
        public string License_Plate { get; private set; }

        public static VehicleDb Create(IVehicle vehicle) => new() { Id = vehicle.Id, Customer_Document = vehicle.CustomerDocument.Id, Brand = vehicle.Brand, Model = vehicle.Model, Year = vehicle.Year, License_Plate = vehicle.LicensePlate.License };

        public IVehicle ToDomain() => new Vehicle(Id, Customer_Document, Brand, Model, Year, License_Plate);
    }
}
