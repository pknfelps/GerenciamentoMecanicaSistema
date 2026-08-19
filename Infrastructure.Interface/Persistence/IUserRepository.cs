using Domain.Interface.User;

namespace Infrastructure.Interface.Persistence
{
    public interface IUserRepository
    {
        Task<int> RegisterUser(IUserCredentials credentials);
        Task<IUser?> GetUser(string name, string role);
        Task<IUserCredentials?> GetUserCredentials(string name, string role);
    }
}
