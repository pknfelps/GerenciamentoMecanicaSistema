using Domain.Interface.Exceptions;
using Domain.Interface.User;

namespace Domain.User
{
    public class User : IUser
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public IPassword Password { get; private set; }
        public Roles Role { get; private set; }

        public User(string name, string password, string role) : this(Guid.NewGuid(), name, password, role) { }

        public User(Guid id, string name, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainValidationException("Nome deve ser preenchido");
            if (string.IsNullOrWhiteSpace(password))
                throw new DomainValidationException("Senha deve ser preenchida");
            if (string.IsNullOrWhiteSpace(role))
                throw new DomainValidationException("Cargo deve ser preenchido");

            if (password == name || password == role)
                throw new DomainValidationException("Senha deve ser diferente do nome e do cargo");

            if (!Enum.TryParse(role, out Roles cargoParsed))
                throw new DomainValidationException($"Cargo \"{role}\" inválido");

            Id = id;
            Name = name;
            Password = new Password(password);
            Role = cargoParsed;
        }
    }
}