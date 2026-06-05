namespace Domain.Costumer
{
    public class Cnpj : Documento
    {
        public const int DigitCount = 14;

        protected override int DocumentDigitCount { get; set; } = DigitCount;

        public Cnpj(string id) : base(id)
        {
            Id = NormalizeDocument(id);
        }

        protected sealed override string NormalizeDocument(string document)
        {
            return $"{document[..2]}.{document[2..5]}.{document[5..8]}/{document[8..12]}-{document[12..]}";
        }
    }
}
