using Domain.Interface.User;

namespace Domain.User
{
    public class Password : ISenha
    {
        public string Senha { get; private set; }

        private static readonly int MinPasswordLenght = 6;

        public Password(string senha)
        {
            ArgumentException.ThrowIfNullOrEmpty(senha);

            if (senha.Contains(' '))
                throw new ArgumentException("Senha não deve conter espaços em branco");

            if (senha.Length < MinPasswordLenght)
                throw new ArgumentException("Senha deve conter mais de 4 caracteres");

            if (senha.FirstOrDefault(char.IsLetter) == default || senha.FirstOrDefault(char.IsNumber) == default || (senha.FirstOrDefault(x => char.IsSymbol(x) || char.IsPunctuation(x)) == default))
                throw new ArgumentException("Senha deve conter letras, números e símbolos");

            if (senha.FirstOrDefault(x => char.IsLetter(x) && char.IsUpper(x)) == default || senha.FirstOrDefault(x => char.IsLetter(x) && char.IsLower(x)) == default)
                throw new ArgumentException("Senha deve conter letras maiúsculas e minúsculas");

            Senha = senha;
        }
    }
}
