namespace Domain.Interface
{
    public interface IUsuario : IEntity
    {
        string Nome { get; }
        ISenha Senha { get; }
        Cargos Cargo { get; }
    }
}
