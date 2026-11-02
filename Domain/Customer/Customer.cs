using Domain.Interface.Exceptions;
using Domain.Interface.Custumer;

namespace Domain.Customer
{
    public class Customer : ICustomer
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public CustomerStatus Status { get; private set; }
        public IDocument Document { get; private set; }
        public IPhone Phone { get; private set; }
        public IEmail Email { get; private set; }

        public Customer(string name, string document, string phone, string email) : this(Guid.NewGuid(), name, document, phone, email, CustomerStatus.Active) { }

        public Customer(Guid id, string name, string document, string phone, string email, CustomerStatus status)
        {
            if (id == Guid.Empty)
                throw new DomainValidationException("O ID do cliente deve ser preenchido");

            if (string.IsNullOrWhiteSpace(name))
                throw new DomainValidationException("O nome do cliente deve ser preenchido");

            if (!Enum.IsDefined(status))
                throw new DomainValidationException("O status do cliente é inválido");

            Id = id;
            Name = name;
            Status = status;
            Document = DocumentWrapper.CreateDocument(document);
            Phone = new Phone(phone);
            Email = new Email(email);
        }
    }
}
