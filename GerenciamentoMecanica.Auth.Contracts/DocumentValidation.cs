namespace GerenciamentoMecanica.Auth.Contracts;

internal static class DocumentValidation
{
    internal static string GetDigits(string? document, string label)
    {
        if (string.IsNullOrWhiteSpace(document))
            throw new FormatException($"{label} deve ser preenchido.");
        if (document.Contains(' '))
            throw new FormatException($"{label} não pode ter espaços em branco.");
        if (document.Any(char.IsLetter))
            throw new FormatException($"{label} não deve conter letras.");

        return string.Concat(document.Where(char.IsNumber));
    }

    internal static string Validate(string? document, string label, int digitCount, int initialMultiplier)
    {
        var digits = GetDigits(document, label);
        if (digits.Length != digitCount)
            throw new FormatException($"{label} inválido.");

        var numbers = digits.Select(x => int.Parse($"{x}")).ToArray();
        for (var verifier = 0; verifier < 2; verifier++)
        {
            var sum = 0;
            for (var index = 0; index < digitCount - 2 + verifier; index++)
            {
                var multiplier = initialMultiplier + verifier - index;
                if (multiplier < 2)
                    multiplier += 8;
                sum += numbers[index] * multiplier;
            }
            var remainder = sum % 11;
            var expected = remainder < 2 ? 0 : 11 - remainder;
            if (numbers[digitCount - 2 + verifier] != expected)
                throw new FormatException($"{label} inválido.");
        }
        return digits;
    }
}
