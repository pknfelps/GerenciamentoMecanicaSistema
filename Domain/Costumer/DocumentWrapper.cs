namespace Domain.Costumer
{
    public static class DocumentWrapper
    {
        private const int CpfDigitCount = 11;
        private const int CnpjDigitCount = 14;

        public static Documento CreateDocument(string document)
        {
            string numbers = string.Concat(document.Where(char.IsNumber));

            return numbers.Length switch
            {
                CpfDigitCount => new Cpf(numbers),
                CnpjDigitCount => new Cnpj(numbers),
                _ => throw new ArgumentException("Documento inválido.", nameof(document))
            };
        }
    }
}
