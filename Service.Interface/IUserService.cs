using Service.Interface.Commands.User;
using Service.Interface.Results.User;

namespace Service.Interface
{
    public interface IUserService
    {
        Task RegisterUser(CreateUserCommand user);
        Task<UserResult?> GetUser(string name = "", string role = "");
        Task<UserResult?> GetOwnUser(Guid userId, string name = "", string role = "");
        Task UpdateOwnUser(Guid userId, UpdateOwnUserCommand command);
    }
}
