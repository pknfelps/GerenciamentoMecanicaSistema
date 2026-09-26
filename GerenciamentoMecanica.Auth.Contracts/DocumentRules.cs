namespace GerenciamentoMecanica.Auth.Contracts;

public enum DocumentType
{
    Cpf,
    Cnpj
}

public sealed record NormalizedDocument(DocumentType Type, string Value);

/// <summary>Identifica, valida e normaliza o documento de cliente (CPF ou CNPJ numérico).</summary>
public static class DocumentRules
{
    public static string Normalize(string? document) => Parse(document).Value;

    /// <exception cref="FormatException">Documento ausente, inválido ou de tipo não suportado.</exception>
    public static NormalizedDocument Parse(string? document)
    {
        var digits = DocumentValidation.GetDigits(document, "Documento");
        return digits.Length switch
        {
            CpfRules.DigitCount => new(DocumentType.Cpf, CpfRules.Normalize(document)),
            CnpjRules.DigitCount => new(DocumentType.Cnpj, CnpjRules.Normalize(document)),
            _ => throw new FormatException("Documento inválido.")
        };
    }
}
