using Domain.Interface.Order;
using Domain.Interface.Service;
using Domain.Interface.Stock;
using Domain.Order;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Repository.Dto
{
    internal class WorkOrderDb
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
        public List<PartDb> Parts { get; private set; }
        [JsonPropertyName("budget")]
        public double Budget { get; private set; }
        [JsonPropertyName("status")]
        public string Status { get; private set; }
        [JsonPropertyName("date_created")]
        public DateTime Date_Created { get; private set; }
        [JsonPropertyName("date_finished")]
        public DateTime Date_Finished { get; private set; }

        public WorkOrderDb(Guid id, string customer_document, string vehicle_license_plate, double budget, string status, DateTime date_created, DateTime date_finished) : this(id, customer_document, vehicle_license_plate, [], [], budget, status, date_created, date_finished) { }

        public WorkOrderDb(Guid id, string customer_document, string vehicle_license_plate, string services, string parts, double budget, string status, DateTime date_created, DateTime date_finished)
        {
            Id = id;
            Customer_Document = customer_document;
            Vehicle_License_Plate = vehicle_license_plate;
            Services = JsonSerializer.Deserialize<List<MechanicalServiceDb>>(services) ?? [];
            Parts = JsonSerializer.Deserialize<List<PartDb>>(parts) ?? [];
            Budget = budget;
            Status = status;
            Date_Created = date_created;
            Date_Finished = date_finished;
        }

        public WorkOrderDb(Guid id, string customer_document, string vehicle_license_plate, List<MechanicalServiceDb> services, List<PartDb> parts, double budget, string status, DateTime date_created, DateTime date_finished)
        {
            Id = id;
            Customer_Document = customer_document;
            Vehicle_License_Plate = vehicle_license_plate;
            Services = services;
            Parts = parts;
            Budget = budget;
            Status = status;
            Date_Created = date_created;
            Date_Finished = date_finished;
        }

        public static WorkOrderDb Create(IWorkOrder order) => new(order.Id, order.CustomerDocument.Id, order.VehicleLicensePlate.License, [.. order.Services.Select(MechanicalServiceDb.Create)], [.. order.Parts.Select(PartDb.Create)], order.Budget, order.Status.ToString(), order.DateCreated, order.DateFinished);

        public IWorkOrder ToDomain()
        {
            var services = Services == null ? new List<IMechanicalService>() : [.. Services.Select(service => service.ToDomain())];
            var parts = Services == null ? new List<IPart>() : [.. Parts.Select(part => part.ToDomain())];

            return new WorkOrder(Id, Customer_Document, Vehicle_License_Plate, services, parts, Budget, Enum.Parse<WorkOrderStatus>(Status), Date_Created, Date_Finished);
        }
    }
}
