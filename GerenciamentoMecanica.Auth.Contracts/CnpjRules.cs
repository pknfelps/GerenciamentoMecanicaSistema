namespace GerenciamentoMecanica.Auth.Contracts;

/// <summary>Validação do CNPJ numérico e representação canônica usadas pelo cadastro.</summary>
public static class CnpjRules
{
    public const int DigitCount = 14;

    /// <exception cref="FormatException">O documento não atende às regras de CNPJ.</exception>
    public static string Normalize(string? document)
    {
        var digits = DocumentValidation.Validate(document, "CNPJ", DigitCount, 5);
        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }
}
