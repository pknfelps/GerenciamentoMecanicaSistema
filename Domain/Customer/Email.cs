using Domain.Interface.Exceptions;
using Domain.Interface.Custumer;

namespace Domain.Customer
{
    public class Email : IEmail
    {
        public string Address { get; private set; }

        public Email(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new DomainValidationException($"E-mail deve ser preenchido.");

            if (address.Contains(' '))
                throw new DomainValidationException($"E-mail não pode ter espaços em branco.");

            if (!address.Contains('@'))
                throw new DomainValidationException("E-mail inválido.");

            Address = address;
        }
    }
}