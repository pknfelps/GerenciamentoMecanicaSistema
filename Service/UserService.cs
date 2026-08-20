using Domain.Interface.User;
using Domain.User;
using Service.Interface.Authentication;
using Service.Interface.Persistence;
using Service.Interface;
using Service.Interface.Exceptions;
using Service.Interface.Commands.User;
using Service.Interface.Results.User;

namespace Service
{
    public class UserService(IUserRepository repository, IPasswordHasher passwordHasher) : IUserService
    {
        private IUserRepository Repository { get; set; } = repository;
        private IPasswordHasher PasswordHasher { get; set; } = passwordHasher;

        public async Task RegisterUser(CreateUserCommand user)
        {
            if (await Repository.GetUser(user.Name, user.Role.ToString()) != null)
                throw new ConflictException("Usuario jÃ¡ cadastrado no sistema");

            var credentials = CreateCredentials(user);
            var registry = await Repository.RegisterUser(credentials);

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

        private IUserCredentials CreateCredentials(CreateUserCommand command)
        {
            var user = new User(command.Name, command.Role);
            var password = new Password(command.Password, user);
            var passwordHash = PasswordHasher.Hash(password.Value);
            return new UserCredentials(user, passwordHash);
        }
    }
}
