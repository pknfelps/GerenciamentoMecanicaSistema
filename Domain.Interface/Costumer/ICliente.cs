namespace Domain.Interface.Costumer
{
    public interface ICliente
    {
        Guid Id { get; }
        string Nome{ get; }
        IDocumento Documento { get; }
        ICelular Celular { get; }
        IEmail Email { get; }
    }
}
