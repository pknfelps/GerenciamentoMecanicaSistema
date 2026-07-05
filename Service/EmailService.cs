using Domain.Interface.Custumer;
using Domain.Interface.Order;
using Domain.Interface.Vehicle;
using Service.Interface;

namespace Service
{
    public class EmailService(IEmailSender emailSender) : IEmailService
    {
        private IEmailSender EmailSender { get; set; } = emailSender;

        public async Task NotifyBudget(ICustomer customer, IVehicle vehicle, IOrder order)
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
    }
}
