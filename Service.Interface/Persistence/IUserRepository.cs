using Domain.Interface.User;

namespace Service.Interface.Persistence
{
    public interface IUserRepository
    {
        Task<int> RegisterUser(IUserCredentials credentials);
        Task<IUser?> GetUser(string name, string role);
        Task<IUser?> GetUserById(Guid id);
        Task<IUserCredentials?> GetUserCredentials(string name, string role);
    }
}
