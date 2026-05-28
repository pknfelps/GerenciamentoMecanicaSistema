namespace Repository.Dto
{
    internal record ClienteDb(Guid Id, string Nome, string Documento, string Celular, string Email)
    {
        public Guid Id { get; private set; } = Id;
        public string Nome { get; private set; } = Nome;
        public string Documento { get; private set; } = Documento;
        public string Celular { get; private set; } = Celular;
        public string Email { get; private set; } = Email;
    }
}
