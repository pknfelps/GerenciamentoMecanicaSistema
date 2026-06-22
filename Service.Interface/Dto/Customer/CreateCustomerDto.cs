using Domain.Interface.Custumer;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Customer
{
    public class CreateCustomerDto(string name, string document, string phone, string email)
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

        public virtual ICustomer ToDomain() => new Domain.Customer.Customer(Name, Document, Phone, Email);

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var costumer = (CreateCustomerDto)obj;

            return Name == costumer.Name && Document == costumer.Document && Phone == costumer.Phone && Email == costumer.Email;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Document, Phone, Email);
        }
    }
}
