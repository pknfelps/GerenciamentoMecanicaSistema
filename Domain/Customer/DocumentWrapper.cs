using Domain.Interface.Exceptions;
using GerenciamentoMecanica.Auth.Contracts;

namespace Domain.Customer
{
    public static class DocumentWrapper
    {
        public static Document CreateDocument(string document)
        {
            NormalizedDocument normalized;
            try
            {
                normalized = DocumentRules.Parse(document);
            }
            catch (FormatException exception)
            {
                throw new DomainValidationException(exception.Message, exception);
            }

            return normalized.Type switch
            {
                DocumentType.Cpf => new Cpf(normalized.Value),
                DocumentType.Cnpj => new Cnpj(normalized.Value),
                _ => throw new DomainValidationException("Documento inválido.")
            };
        }
    }
}
