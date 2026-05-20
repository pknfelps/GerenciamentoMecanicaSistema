using Domain.Interface.Costumer;

namespace Domain.Costumer
{
    public class Email : IEmail
    {
        public string Endereco { get; private set; }

        public Email(string endereco)
        {
            if (string.IsNullOrWhiteSpace(endereco))
                throw new ArgumentNullException(nameof(endereco), $"E-mail deve ser preenchido.");

            if (endereco.Contains(' '))
                throw new ArgumentNullException(nameof(endereco), $"E-mail não pode ter espaços em branco.");

            if (!endereco.Contains('@'))
                throw new ArgumentException("E-mail inválido.", nameof(endereco));

            Endereco = endereco;
        }
    }
}
