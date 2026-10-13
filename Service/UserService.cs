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
            UserRoleRules.Parse(user.Role);

            if (await Repository.GetUser(user.Name, user.Role.ToString()) != null)
                throw new ConflictException("Usuário já cadastrado no sistema");

            var credentials = CreateCredentials(user);
            var registry = await Repository.RegisterUser(credentials);

            if (registry == 0)
                throw new ApplicationFailureException("Falha ao cadastrar o usuário");
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

        public async Task<UserResult?> GetOwnUser(Guid userId, string name = "", string role = "")
        {
            if (userId == Guid.Empty)
                throw new InvalidRequestException("A identificação do usuário deve ser informada");

            var user = await Repository.GetUserById(userId);
            if (user is null)
                return null;

            if ((!string.IsNullOrEmpty(name) && name != user.Name)
                || (!string.IsNullOrEmpty(role) && role != user.Role.ToString()))
                throw new AccessDeniedException("A consulta deve se limitar à própria conta");

            return UserResult.Create(user);
        }

        public async Task UpdateOwnUser(Guid userId, UpdateOwnUserCommand command)
        {
            if (userId == Guid.Empty)
                throw new InvalidRequestException("A identificação do usuário deve ser informada");
            if (command.Name is null && command.Password is null)
                throw new InvalidRequestException("Informe nome ou senha para atualizar a conta");
            if (command.Name is not null && (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 100))
                throw new InvalidRequestException("O nome deve ser preenchido e ter no máximo 100 caracteres");

            var current = await Repository.GetUserById(userId)
                ?? throw new NotFoundException("Usuário não encontrado");
            var user = new User(current.Id, command.Name ?? current.Name, current.Role.ToString());

            if (command.Name is not null)
            {
                var existing = await Repository.GetUser(user.Name, user.Role.ToString());
                if (existing is not null && existing.Id != user.Id)
                    throw new ConflictException("Nome já utilizado por outro usuário deste perfil");
            }

            string? passwordHash = null;
            if (command.Password is not null)
            {
                var password = new Password(command.Password, user);
                passwordHash = PasswordHasher.Hash(password.Value);
            }

            if (await Repository.UpdateUser(user.Id, command.Name, passwordHash) == 0)
                throw new NotFoundException("Usuário não encontrado");
        }
    }
}
