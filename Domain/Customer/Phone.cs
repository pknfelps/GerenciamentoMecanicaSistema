using Domain.Interface.Custumer;

namespace Domain.Customer
{
    public class Phone : IPhone
    {
        public string Number { get; private set; }

        private readonly int NumberDigitCount = 11;

        public Phone(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
                throw new ArgumentNullException(nameof(number), $"Número de celular deve ser preenchido.");

            if (number.FirstOrDefault(char.IsLetter) != default)
                throw new ArgumentException($"Número de celular não deve conter letras.", nameof(number));

            string numbers = string.Concat(number.Where(char.IsNumber));

            if (numbers.Length != NumberDigitCount)
                throw new ArgumentException($"Número de celular inválido.", nameof(number));

            Number = $"({numbers[..2]}) {numbers[2..7]}-{numbers[7..]}";
        }
    }
}
