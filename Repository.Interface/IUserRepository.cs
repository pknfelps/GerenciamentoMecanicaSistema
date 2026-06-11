using Domain.Interface.User;

namespace Repository.Interface
{
    public interface IUserRepository
    {
        Task<int> RegisterUser(IUser user);
        Task<IUser?> GetUser(string name, string role);
    }
}
