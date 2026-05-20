using Domain.Interface.Costumer;

namespace Domain.Costumer
{
    public abstract class Documento : IDocumento
    {
        public string Id { get; private set; }

        protected abstract int DocumentDigitCount { get; set; }

        public Documento(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                throw new ArgumentNullException(nameof(documento), $"{this} deve ser preenchido.");

            if (documento.Contains(' '))
                throw new ArgumentNullException(nameof(documento), $"{this} não pode ter espaços em branco.");

            if (documento.FirstOrDefault(char.IsLetter) != default)
                throw new ArgumentException($"{this} não deve conter letras.", nameof(documento));

            string numbers = string.Concat(documento.Where(char.IsNumber));

            if (numbers.Length != DocumentDigitCount)
                throw new ArgumentException($"{this} inválido.", nameof(documento));

            Id = NormalizeDocument(numbers);
        }

        protected abstract string NormalizeDocument(string document);
    }
}
