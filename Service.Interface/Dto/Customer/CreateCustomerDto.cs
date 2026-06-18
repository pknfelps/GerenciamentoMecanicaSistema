using Domain.Interface.Custumer;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Customer
{
    public class CreateCustomerDto(string name, string document, string phone, string email)
    {
        [Required, RegularExpression(@"^[a-zA-ZÀ-ÿ\s]{3,}$")]
        public string Name { get; set; } = name;
        [Required, RegularDocumentExpression]
        public string Document { get; set; } = document;
        [Required, RegularExpression(@"(?:\D*\d){11}")]
        public string Phone { get; set; } = phone;
        [Required, RegularExpression(@"^[^\s]+\@[^\s]+\.[^\s]+$")]
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
