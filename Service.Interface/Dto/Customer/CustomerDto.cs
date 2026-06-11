using Domain.Interface.Custumer;
using Service.Interface.Dto.CustomAttributes;
using System.ComponentModel.DataAnnotations;

namespace Service.Interface.Dto.Customer
{
    public class CustomerDto(Guid id, string name, string document, string phone, string email) : CreateCustomerDto(name, document, phone, email)
    {
        [Required, GuidValidation]
        public Guid Id { get; private set; } = id;

        public static CustomerDto Create(ICustomer customer) => new(customer.Id, customer.Name, customer.Document.Id, customer.Phone.Number, customer.Email.Address);

        public override ICustomer ToDomain() => new Domain.Customer.Customer(Id, Name, Document, Phone, Email);

        public override bool Equals(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var customer = (CustomerDto)obj;

            return Id == customer.Id && Name == customer.Name && Document == customer.Document && Phone == customer.Phone && Email == customer.Email;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Document, Phone, Email);
        }
    }
}
