using Domain.Interface;

namespace Domain
{
    public class Usuario : IUsuario
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; }
        public ISenha Senha { get; private set; }
        public Cargos Cargo { get; private set; }

        public Usuario(string nome, string senha, string cargo) : this(Guid.NewGuid(), nome, senha, cargo) { }

        public Usuario(Guid id, string nome, string senha, string cargo)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nome);
            ArgumentException.ThrowIfNullOrWhiteSpace(senha);
            ArgumentException.ThrowIfNullOrWhiteSpace(cargo);

            if (senha == nome || senha == cargo)
                throw new ArgumentException("Senha deve ser diferente do nome e do cargo");

            if (!Enum.TryParse(cargo, out Cargos cargoParsed))
                throw new ArgumentException($"Cargo \"{cargo}\" inválido");

            Id = id;
            Nome = nome;
            Senha = new Password(senha);
            Cargo = cargoParsed;
        }
    }
}
