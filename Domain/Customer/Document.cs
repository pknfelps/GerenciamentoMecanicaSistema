using Domain.Interface.Exceptions;
using Domain.Interface.Custumer;
using System.Reflection.Metadata;

namespace Domain.Customer
{
    public abstract class Document : IDocument
    {
        public string Id { get; protected set; }

        protected abstract int DocumentDigitCount { get; set; }
        protected abstract int InitialVerifierDigitMultiplier { get; set; }

        protected Document(string document)
        {
            if (string.IsNullOrWhiteSpace(document))
                throw new DomainValidationException($"{this} deve ser preenchido.");

            if (document.Contains(' '))
                throw new DomainValidationException($"{this} não pode ter espaços em branco.");

            if (document.FirstOrDefault(char.IsLetter) != default)
                throw new DomainValidationException($"{this} não deve conter letras.");

            string numbers = string.Concat(document.Where(char.IsNumber));

            if (numbers.Length != DocumentDigitCount)
                throw new DomainValidationException($"{this} inválido.");

            if (!IsValid(numbers))
                throw new DomainValidationException($"{this} inválido.");

            Id = numbers;
        }

        protected abstract string NormalizeDocument(string document);

        private bool IsValid(string document)
        {
            List<int> numbers = [.. document.Select(x => int.Parse($"{x}"))];

            if (!IsDigitValid(numbers, 0))
                return false;

            if (!IsDigitValid(numbers, 1))
                return false;

            return true;
        }

        private bool IsDigitValid(List<int> numbers, int digitIndex)
        {
            int sum = 0;

            for (int i = 0; i < DocumentDigitCount - (2 - digitIndex); i++)
            {
                int current = (InitialVerifierDigitMultiplier + digitIndex) - i;
                int multiplier = current < 2 ? current + 8 : current;
                sum += numbers[i] * multiplier;
            }

            int module = sum % 11;
            int digit = module < 2 ? 0 : 11 - module;

            if (numbers[DocumentDigitCount - (2 - digitIndex)] != digit)
                return false;

            return true;
        }
    }
}