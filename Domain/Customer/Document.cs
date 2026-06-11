using Domain.Interface.Custumer;

namespace Domain.Customer
{
    public abstract class Document : IDocument
    {
        public string Id { get; protected set; }

        protected abstract int DocumentDigitCount { get; set; }

        protected Document(string document)
        {
            if (string.IsNullOrWhiteSpace(document))
                throw new ArgumentNullException(nameof(document), $"{this} deve ser preenchido.");

            if (document.Contains(' '))
                throw new ArgumentNullException(nameof(document), $"{this} não pode ter espaços em branco.");

            if (document.FirstOrDefault(char.IsLetter) != default)
                throw new ArgumentException($"{this} não deve conter letras.", nameof(document));

            string numbers = string.Concat(document.Where(char.IsNumber));

            if (numbers.Length != DocumentDigitCount)
                throw new ArgumentException($"{this} inválido.", nameof(document));

            Id = numbers;
        }

        protected abstract string NormalizeDocument(string document);
    }
}
