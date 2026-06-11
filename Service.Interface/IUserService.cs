using Service.Interface.Dto.User;

namespace Service.Interface
{
    public interface IUserService
    {
        Task RegisterUser(CreateUserDto userDto);
        Task<UserDto?> GetUser(CreateUserDto userDto);
    }
}
