using Domain.Interface.User;

namespace Service.Interface.Persistence
{
    public interface IUserRepository
    {
        Task<int> RegisterUser(IUserCredentials credentials);
        Task<IUser?> GetUser(string name, string role);
        Task<IUser?> GetUserById(Guid id);
        Task<int> UpdateUser(Guid id, string? name, string? passwordHash);
        Task<IUserCredentials?> GetUserCredentials(string name, string role);
    }
}
