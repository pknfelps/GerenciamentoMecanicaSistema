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
            var user = userDto.ToDomain();

            if (await CheckIfUsuarioExists(user.Name, user.Role.ToString()))
                throw new InvalidOperationException("Usuario já cadastrado no sistema");

            var registry = await Repository.RegisterUser(user);

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

        private async Task<bool> CheckIfUsuarioExists(string nome, string cargo)
        {
            return await Repository.GetUser(nome, cargo) != null;
        }
    }
}
