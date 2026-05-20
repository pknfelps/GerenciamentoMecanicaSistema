namespace Domain.Costumer
{
    public class Cpf(string id) : Documento(id)
    {
        protected override int DocumentDigitCount { get; set; } = 11;

        protected override string NormalizeDocument(string document)
        {
            return $"{document[..3]}.{document[3..6]}.{document[6..9]}-{document[9..]}";
        }
    }
}
