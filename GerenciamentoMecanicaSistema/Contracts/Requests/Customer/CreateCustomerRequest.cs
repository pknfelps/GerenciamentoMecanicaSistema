using Service.Interface.Commands.Customer;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GerenciamentoMecanicaSistema.Contracts.Requests.Customer
{
    public class CreateCustomerRequest(string name, string document, string phone, string email)
    {
        [Description("Nome do cliente")]
        [Required, RegularNonEmptyStringExpression]
        public string Name { get; set; } = name;

        [Description("Número do documento do cliente")]
        [Required, RegularDocumentExpression]
        public string Document { get; set; } = document;

        [Description("Número do celular do cliente")]
        [Required, RegularExpression(@"(?:\D*\d){11}", ErrorMessage = "O campo {0} não é um número válido")]
        public string Phone { get; set; } = phone;

        [Description("Endereço de email do cliente")]
        [Required, RegularExpression(@"^[^\s]+\@[^\s]+\.[^\s]+$", ErrorMessage = "O campo {0} não é um email válido")]
        public string Email { get; set; } = email;

        public CreateCustomerCommand ToCommand() => new(Name, Document, Phone, Email);
    }
}
