namespace Domain.Costumer
{
    public class Cnpj(string id) : Documento(id)
    {
        protected override int DocumentDigitCount { get; set; } = 14;

        protected override string NormalizeDocument(string document)
        {
            return $"{document[..2]}.{document[2..5]}.{document[5..8]}/{document[8..12]}-{document[12..]}";
        }
    }
}
