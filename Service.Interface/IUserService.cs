using Service.Interface.Commands.User;
using Service.Interface.Results.User;

namespace Service.Interface
{
    public interface IUserService
    {
        Task RegisterUser(CreateUserCommand user);
        Task<UserResult?> GetUser(CreateUserCommand user);
    }
}
