namespace Domain.Costumer
{
    public class Cpf : Documento
    {
        protected override int DocumentDigitCount { get; set; } = 11;

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
