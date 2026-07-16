using Domain.Interface.Exceptions;
namespace Domain.Customer
{
    public static class DocumentWrapper
    {
        public static Document CreateDocument(string document)
        {
            string numbers = string.Concat(document.Where(char.IsNumber));

            return numbers.Length switch
            {
                Cpf.DigitCount => new Cpf(numbers),
                Cnpj.DigitCount => new Cnpj(numbers),
                _ => throw new DomainValidationException("Documento inválido.")
            };
        }
    }
}