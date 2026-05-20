using Domain.Interface.Costumer;

namespace Domain.Costumer
{
    public class Celular : ICelular
    {
        public string Numero { get; private set; }

        private readonly int NumberDigitCount = 11;

        public Celular(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentNullException(nameof(numero), $"Número de celular deve ser preenchido.");

            if (numero.FirstOrDefault(char.IsLetter) != default)
                throw new ArgumentException($"Número de celular não deve conter letras.", nameof(numero));

            string numbers = string.Concat(numero.Where(char.IsNumber));

            if (numbers.Length != NumberDigitCount)
                throw new ArgumentException($"Número de celular inválido.", nameof(numero));

            Numero = $"({numbers[..2]}) {numbers[2..7]}-{numbers[7..]}";
        }
    }
}
