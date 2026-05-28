namespace Repository.Dto
{
    internal record UsuarioDb(Guid Id, string Nome, string Senha, string Cargo)
    {
        public Guid Id { get; private set; } = Id;
        public string Nome { get; private set; } = Nome;
        public string Senha { get; private set; } = Senha;
        public string Cargo { get; private set; } = Cargo;
    }
}
