namespace GerenciamentoMecanica.Auth.Contracts;

/// <summary>Validação de dígitos e representação canônica do CPF usada pelo cadastro.</summary>
public static class CpfRules
{
    public const int DigitCount = 11;

    /// <exception cref="FormatException">O documento não atende às regras de CPF.</exception>
    public static string Normalize(string? document)
    {
        var digits = DocumentValidation.Validate(document, "CPF", DigitCount, 10);
        return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
    }
}
