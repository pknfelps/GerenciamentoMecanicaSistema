using Service.Interface.Dto.User;

namespace Service.Interface
{
    public interface IAuthenticationService
    {
        Task<string> Authenticate(CreateUserDto userDto);
    }
}
