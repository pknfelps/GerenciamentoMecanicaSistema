using Domain.Interface.Custumer;

namespace Domain.Customer
{
    public class Customer : ICustomer
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public IDocument Document { get; private set; }
        public IPhone Phone { get; private set; }
        public IEmail Email { get; private set; }

        public Customer(string name, string document, string phone, string email) : this(Guid.NewGuid(), name, document, phone, email) { }

        public Customer(Guid id, string name, string document, string phone, string email)
        {
            if (id == Guid.Empty)
                throw new ArgumentNullException(nameof(id), $"{nameof(id)} deve ser preenchido");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), $"{nameof(name)} deve ser preenchido");

            Id = id;
            Name = name;
            Document = DocumentWrapper.CreateDocument(document);
            Phone = new Phone(phone);
            Email = new Email(email);
        }
    }
}
