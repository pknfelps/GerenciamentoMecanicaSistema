using Domain.Interface.User;

namespace Domain.User
{
    public class Password : IPassword
    {
        public string Secret { get; private set; }

        private static readonly int MinPasswordLenght = 6;

        public Password(string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);

            if (password.Contains(' '))
                throw new ArgumentException("Senha não deve conter espaços em branco");

            if (password.Length < MinPasswordLenght)
                throw new ArgumentException("Senha deve conter mais de 4 caracteres");

            if (password.FirstOrDefault(char.IsLetter) == default || password.FirstOrDefault(char.IsNumber) == default || (password.FirstOrDefault(x => char.IsSymbol(x) || char.IsPunctuation(x)) == default))
                throw new ArgumentException("Senha deve conter letras, números e símbolos");

            if (password.FirstOrDefault(x => char.IsLetter(x) && char.IsUpper(x)) == default || password.FirstOrDefault(x => char.IsLetter(x) && char.IsLower(x)) == default)
                throw new ArgumentException("Senha deve conter letras maiúsculas e minúsculas");

            Secret = password;
        }
    }
}
