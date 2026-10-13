using Service.Interface.Results.Customer;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Customer
{
    public class CustomerResponse(Guid id, string name, string document, string phone, string email, string status)
    {
        public Guid Id { get; set; } = id;
        public string Name { get; set; } = name;
        public string Document { get; set; } = document;
        public string Phone { get; set; } = phone;
        public string Email { get; set; } = email;
        public string Status { get; set; } = status;

        public static CustomerResponse Create(CustomerResult customer) => new(customer.Id, customer.Name, customer.Document, customer.Phone, customer.Email, customer.Status.ToString());
    }
}
