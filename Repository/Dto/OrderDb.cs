using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.WorkOrder;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Repository.Dto
{
    internal class OrderDb
    {
        [JsonPropertyName("id")]
        public Guid Id { get; private set; }
        [JsonPropertyName("customoer_document")]
        public string Customer_Document { get; private set; }
        [JsonPropertyName("vehicle_license_plate")]
        public string Vehicle_License_Plate { get; private set; }
        [JsonPropertyName("services")]
        public List<MechanicalServiceDb> Services { get; private set; }
        [JsonPropertyName("parts")]
        public List<MaterialDb> Materials { get; private set; }
        [JsonPropertyName("budget")]
        public double Budget { get; private set; }
        [JsonPropertyName("status")]
        public string Status { get; private set; }
        [JsonPropertyName("date_created")]
        public DateTime Date_Created { get; private set; }
        [JsonPropertyName("date_finished")]
        public DateTime Date_Finished { get; private set; }
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; private set; }

        // Used by GetOrders or GetCustomerOrders that returns the detailed order
        public OrderDb(Guid id, string customer_document, string vehicle_license_plate, string services, string materials, double budget, string status, DateTime date_created, DateTime date_finished)
        {
            Id = id;
            Customer_Document = customer_document;
            Vehicle_License_Plate = vehicle_license_plate;
            Services = JsonSerializer.Deserialize<List<MechanicalServiceDb>>(services) ?? [];
            Materials = JsonSerializer.Deserialize<List<MaterialDb>>(materials) ?? [];
            Budget = budget;
            Status = status;
            Date_Created = date_created;
            Date_Finished = date_finished;
        }

        public OrderDb(Guid id, string customer_document, string vehicle_license_plate, List<MechanicalServiceDb> services, List<MaterialDb> materials, double budget, string status, DateTime date_created, DateTime date_finished, TimeSpan duration)
        {
            Id = id;
            Customer_Document = customer_document;
            Vehicle_License_Plate = vehicle_license_plate;
            Services = services;
            Materials = materials;
            Budget = budget;
            Status = status;
            Date_Created = date_created;
            Date_Finished = date_finished;
            Duration = duration;
        }

        public static OrderDb Create(IOrder order) => new(order.Id, order.CustomerDocument.Id, order.VehicleLicensePlate.License, [.. order.Services.Select(MechanicalServiceDb.Create)], [.. order.Materials.Select(MaterialDb.Create)], order.Budget, order.Status.ToString(), order.DateCreated, order.DateFinished, order.Duration);

        public IOrder ToDomain()
        {
            var services = Services == null ? new List<IMechanicalService>() : [.. Services.Select(service => service.ToDomain())];
            var materials = Materials == null ? new List<IMaterial>() : [.. Materials.Select(material => material.ToDomain())];

            return new Order(Id, Customer_Document, Vehicle_License_Plate, services, materials, Budget, Enum.Parse<WorkOrderStatus>(Status), Date_Created, Date_Finished);
        }
    }
}
