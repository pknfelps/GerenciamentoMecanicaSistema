using Service.Interface.Dto.CustomAttributes;
using Service.Interface.Results.Order;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Order
{
    public class WorkOrderResponse(Guid id, string customerDocument, string vehicleLicensePlate, double budget, string status, DateTime dateCreated, DateTime dateFinished, TimeSpan duration)
    {
        [Description("Id único da ordem")]
        [Required, GuidValidation]
        public Guid Id { get; set; } = id;

        [Description("Documento do cliente")]
        [Required, RegularDocumentExpression]
        public string CustomerDocument { get; set; } = customerDocument;

        [Description("Placa do veículo da ordem")]
        [Required, RegularLicensePlateExpression]
        public string VehicleLicensePlate { get; set; } = vehicleLicensePlate;

        [Description("Orçamento da ordem")]
        [Required, Range(0, double.MaxValue)]
        public double Budget { get; set; } = budget;

        [Required, RegularNonEmptyStringExpression]
        [Description("Status da ordem")]
        public string Status { get; set; } = status;

        [Description("Data de criação da ordem")]
        [Required]
        public DateTime DateCreated { get; set; } = dateCreated;

        [Description("Data de conclusão da ordem")]
        [Required]
        public DateTime DateFinished { get; set; } = dateFinished;

        [Description("Duração da ordem")]
        [Required]
        public TimeSpan Duration { get; set; } = duration;

        public static WorkOrderResponse Create(WorkOrderResult order) => new(order.Id, order.CustomerDocument, order.VehicleLicensePlate, order.Budget, order.Status, order.DateCreated, order.DateFinished, order.Duration);

        public static WorkOrderResponse Create(DetailedWorkOrderResult order) => Create(WorkOrderResult.Create(order));
    }
}
