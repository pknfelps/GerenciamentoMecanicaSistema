namespace Domain.Interface.User
{
    public interface IUser : IEntity
    {
        string Name { get; }
        Roles Role { get; }
    }
}
