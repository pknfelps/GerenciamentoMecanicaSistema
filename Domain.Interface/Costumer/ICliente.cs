namespace Domain.Interface.Costumer
{
    public interface ICliente
    {
        string Nome{ get; }
        IDocumento Documento { get; }
        ICelular Celular { get; }
        IEmail Email { get; }
    }
}
