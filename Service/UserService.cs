using Domain.Interface.User;
using Domain.User;
using Repository.Interface;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.User;
using Service.Interface.Results.User;

namespace Service
{
    public class UserService(IUserRepository repository) : IUserService
    {
        private IUserRepository Repository { get; set; } = repository;

        public async Task RegisterUser(CreateUserCommand user)
        {
            if (await Repository.GetUser(user.Name, user.Role.ToString()) != null)
                throw new ConflictException("Usuario jÃ¡ cadastrado no sistema");

            var registry = await Repository.RegisterUser(CreateDomain(user));

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao cadastrar o usuÃ¡rio");
        }

        public async Task<UserResult?> GetUser(string name = "", string role = "")
        {
            var registeredUser = await Repository.GetUser(name, role);

            if (registeredUser == null)
                return null;

            return UserResult.Create(registeredUser);
        }

        private static IUser CreateDomain(CreateUserCommand user) => new User(user.Name, user.Password, user.Role);
    }
}
