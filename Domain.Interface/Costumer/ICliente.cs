namespace Domain.Interface.Costumer
{
    public interface ICliente : IEntity
    {
        string Nome{ get; }
        IDocumento Documento { get; }
        ICelular Celular { get; }
        IEmail Email { get; }
    }
}
