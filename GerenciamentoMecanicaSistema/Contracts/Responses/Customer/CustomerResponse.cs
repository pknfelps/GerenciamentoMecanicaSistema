using Service.Interface.Dto.CustomAttributes;
using Service.Interface.Results.Customer;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Responses.Customer
{
    public class CustomerResponse(Guid id, string name, string document, string phone, string email)
    {
        [Description("Id único do cliente no banco de dados")]
        [Required, GuidValidation]
        public Guid Id { get; set; } = id;

        public string Name { get; set; } = name;
        public string Document { get; set; } = document;
        public string Phone { get; set; } = phone;
        public string Email { get; set; } = email;

        public static CustomerResponse Create(CustomerResult customer) => new(customer.Id, customer.Name, customer.Document, customer.Phone, customer.Email);
    }
}
