namespace Domain.Interface.Custumer
{
    public interface ICustomer : IEntity
    {
        string Name { get; }
        CustomerStatus Status { get; }
        IDocument Document { get; }
        IPhone Phone { get; }
        IEmail Email { get; }
    }
}
