using Domain.Interface.Custumer;

namespace Domain.Customer
{
    public class Email : IEmail
    {
        public string Address { get; private set; }

        public Email(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address), $"E-mail deve ser preenchido.");

            if (address.Contains(' '))
                throw new ArgumentNullException(nameof(address), $"E-mail não pode ter espaços em branco.");

            if (!address.Contains('@'))
                throw new ArgumentException("E-mail inválido.", nameof(address));

            Address = address;
        }
    }
}
