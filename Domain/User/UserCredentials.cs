using Domain.Interface.User;

namespace Domain.User
{
    public sealed class UserCredentials : IUserCredentials
    {
        public IUser User { get; }
        public string PasswordHash { get; }

        public UserCredentials(IUser user, string passwordHash)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Hash da senha deve ser preenchido", nameof(passwordHash));

            User = user;
            PasswordHash = passwordHash;
        }
    }
}
