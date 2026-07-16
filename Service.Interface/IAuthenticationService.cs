using Service.Interface.Commands.User;

namespace Service.Interface
{
    public interface IAuthenticationService
    {
        Task<string> Authenticate(CreateUserCommand user);
    }
}
