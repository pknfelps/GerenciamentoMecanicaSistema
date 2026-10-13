using Domain.Interface.Exceptions;
using Domain.Interface.User;

namespace Domain.User
{
    public class User : IUser
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Roles Role { get; private set; }

        public User(string name, string role) : this(Guid.NewGuid(), name, role) { }

        public User(Guid id, string name, string role)
        {
            if (id == Guid.Empty)
                throw new DomainValidationException("O ID do usuário deve ser preenchido");

            if (string.IsNullOrWhiteSpace(name))
                throw new DomainValidationException("Nome deve ser preenchido");

            Id = id;
            Name = name;
            Role = UserRoleRules.Parse(role);
        }
    }
}
