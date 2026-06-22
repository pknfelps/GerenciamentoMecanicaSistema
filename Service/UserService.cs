using Domain.Interface.User;
using Domain.User;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Dto.User;

namespace Service
{
    public class UserService(IUserRepository repository) : IUserService
    {
        private IUserRepository Repository { get; set; } = repository;

        public async Task RegisterUser(CreateUserDto userDto)
        {
            if (await Repository.GetUser(userDto.Name, userDto.Role.ToString()) != null)
                throw new InvalidOperationException("Usuario já cadastrado no sistema");

            var registry = await Repository.RegisterUser(userDto.ToDomain());

            if (registry == 0)
                throw new InvalidOperationException("Falha ao cadastrar o usuário");
        }

        public async Task<UserDto?> GetUser(CreateUserDto userDto)
        {
            var user = await Repository.GetUser(userDto.Name, userDto.Role);

            if (user == null)
                return null;

            return UserDto.Create(user);
        }
    }
}
