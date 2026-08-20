using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Vehicle;
using Service.Interface.Email;
using Service.Interface;
using Service.Interface.Events.Order;

namespace Service
{
    public class EmailService(IEmailSender emailSender) : IEmailService
    {
        private IEmailSender EmailSender { get; set; } = emailSender;

        public async Task NotifyBudget(ICustomer customer, IVehicle vehicle, OrderNotificationSnapshot order)
        {
            var body = $"""
                Olá {customer.Name}. Gostaríamos de informar que o orçamento do serviço no seu {vehicle.Model} com a placa {vehicle.LicensePlate.License} ficou no valor de R${order.Budget}.

                Atenciosamente, Mecânica.
                """;

            await EmailSender.SendAsync(
                customer.Name,
                customer.Email.Address,
                "Serviço Finalizado",
                body,
                body);
        }

        public async Task NotifyOrderStatus(ICustomer customer, IVehicle vehicle, OrderNotificationSnapshot order)
        {
            var status = GetStatusDescription(order.Status);
            var body = $"""
                Olá {customer.Name}. A ordem de serviço {order.OrderId}, referente ao seu {vehicle.Model} de placa {vehicle.LicensePlate.License}, agora está {status}.

                Atenciosamente, Mecânica.
                """;

            await EmailSender.SendAsync(
                customer.Name,
                customer.Email.Address,
                $"Atualização da ordem de serviço {order.OrderId}",
                body,
                body);
        }

        private static string GetStatusDescription(WorkOrderStatus status) =>
            status switch
            {
                WorkOrderStatus.Received => "recebida",
                WorkOrderStatus.InDiagnosis => "em diagnóstico",
                WorkOrderStatus.WaitingForApproval => "aguardando aprovação do orçamento",
                WorkOrderStatus.WaitingForExecution => "aguardando o início da execução",
                WorkOrderStatus.InExecution => "em execução",
                WorkOrderStatus.Finished => "finalizada",
                WorkOrderStatus.Delivered => "entregue",
                _ => status.ToString()
            };
    }
}
