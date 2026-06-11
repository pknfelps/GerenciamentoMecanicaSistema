namespace Domain.Interface.User
{
    public interface IUser : IEntity
    {
        string Name { get; }
        IPassword Password { get; }
        Roles Role { get; }
    }
}
