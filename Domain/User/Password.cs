using Domain.Interface.Exceptions;
using Domain.Interface.User;

namespace Domain.User
{
    public class Password : IPassword
    {
        public string Value { get; private set; }

        private const int MinPasswordLength = 6;

        public Password(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new DomainValidationException("Senha deve ser preenchida");

            if (password.Any(char.IsWhiteSpace))
                throw new DomainValidationException("Senha não deve conter espaços em branco");

            if (password.Length < MinPasswordLength)
                throw new DomainValidationException($"Senha deve conter pelo menos {MinPasswordLength} caracteres");

            if (!password.Any(char.IsLetter)
                || !password.Any(char.IsDigit)
                || !password.Any(character => char.IsSymbol(character) || char.IsPunctuation(character)))
                throw new DomainValidationException("Senha deve conter letras, números e símbolos");

            if (!password.Any(char.IsUpper) || !password.Any(char.IsLower))
                throw new DomainValidationException("Senha deve conter letras maiúsculas e minúsculas");

            Value = password;
        }

        public Password(string password, IUser user) : this(password)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (Value == user.Name || Value == user.Role.ToString())
                throw new DomainValidationException("Senha deve ser diferente do nome e do cargo");
        }
    }
}
