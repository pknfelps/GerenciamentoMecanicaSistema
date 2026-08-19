namespace Domain.Interface.User
{
    public interface IUserCredentials
    {
        IUser User { get; }
        string PasswordHash { get; }
    }
}
