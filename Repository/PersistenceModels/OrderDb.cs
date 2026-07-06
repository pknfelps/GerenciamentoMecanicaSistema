using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.WorkOrder;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Repository.PersistenceModels
{
    internal class OrderDb
    {
        [JsonPropertyName("id")]
        public Guid Id { get; private set; }
        [JsonPropertyName("customoer_document")]
        public string CustomerDocument { get; private set; }
        [JsonPropertyName("vehicle_license_plate")]
        public string VehicleLicensePlate { get; private set; }
        [JsonPropertyName("services")]
        public List<MechanicalServiceDb> Services { get; private set; }
        [JsonPropertyName("parts")]
        public List<MaterialDb> Materials { get; private set; }
        [JsonPropertyName("budget")]
        public double Budget { get; private set; }
        [JsonPropertyName("status")]
        public string Status { get; private set; }
        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; private set; }
        [JsonPropertyName("date_finished")]
        public DateTime DateFinished { get; private set; }
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; private set; }

        // Used by GetOrders or GetCustomerOrders that returns the detailed order
        public OrderDb(Guid id, string customerDocument, string vehicleLicensePlate, string services, string materials, double budget, string status, DateTime dateCreated, DateTime dateFinished)
        {
            Id = id;
            CustomerDocument = customerDocument;
            VehicleLicensePlate = vehicleLicensePlate;
            Services = JsonSerializer.Deserialize<List<MechanicalServiceDb>>(services) ?? [];
            Materials = JsonSerializer.Deserialize<List<MaterialDb>>(materials) ?? [];
            Budget = budget;
            Status = status;
            DateCreated = dateCreated;
            DateFinished = dateFinished;
        }

        public OrderDb(Guid id, string customerDocument, string vehicleLicensePlate, List<MechanicalServiceDb> services, List<MaterialDb> materials, double budget, string status, DateTime dateCreated, DateTime dateFinished, TimeSpan duration)
        {
            Id = id;
            CustomerDocument = customerDocument;
            VehicleLicensePlate = vehicleLicensePlate;
            Services = services;
            Materials = materials;
            Budget = budget;
            Status = status;
            DateCreated = dateCreated;
            DateFinished = dateFinished;
            Duration = duration;
        }

        public static OrderDb Create(IOrder order) => new(order.Id, order.CustomerDocument.Id, order.VehicleLicensePlate.License, [.. order.Services.Select(MechanicalServiceDb.Create)], [.. order.Materials.Select(MaterialDb.Create)], order.Budget, order.Status.ToString(), order.DateCreated, order.DateFinished, order.Duration);

        public IOrder ToDomain()
        {
            var services = Services == null ? new List<IMechanicalService>() : [.. Services.Select(service => service.ToDomain())];
            var materials = Materials == null ? new List<IMaterial>() : [.. Materials.Select(material => material.ToDomain())];

            return new Order(Id, CustomerDocument, VehicleLicensePlate, services, materials, Budget, Enum.Parse<WorkOrderStatus>(Status), DateCreated, DateFinished);
        }
    }
}
