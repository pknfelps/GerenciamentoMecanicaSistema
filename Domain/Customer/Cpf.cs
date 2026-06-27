namespace Domain.Customer
{
    public class Cpf : Document
    {
        public const int DigitCount = 11;

        protected override int DocumentDigitCount { get; set; } = DigitCount;
        protected override int InitialVerifierDigitMultiplier { get; set; } = 10;

        public Cpf(string id) : base(id) 
        {
            Id = NormalizeDocument(id);
        }

        protected sealed override string NormalizeDocument(string document)
        {
            return $"{document[..3]}.{document[3..6]}.{document[6..9]}-{document[9..]}";
        }
    }
}
