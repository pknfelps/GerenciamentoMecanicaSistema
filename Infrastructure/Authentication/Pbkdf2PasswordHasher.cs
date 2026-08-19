using System.Security.Cryptography;
using Infrastructure.Interface.Authentication;

namespace Infrastructure.Authentication
{
    public sealed class Pbkdf2PasswordHasher : IPasswordHasher
    {
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const string Algorithm = "pbkdf2-sha256";

        public string Hash(string password)
        {
            ArgumentNullException.ThrowIfNull(password);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public bool Verify(string password, string passwordHash)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(passwordHash);

            var parts = passwordHash.Split('$');
            if (parts.Length != 4
                || parts[0] != Algorithm
                || !int.TryParse(parts[1], out var iterations)
                || iterations != Iterations)
                return false;

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expectedHash = Convert.FromBase64String(parts[3]);
                if (salt.Length != SaltSize || expectedHash.Length != HashSize)
                    return false;

                var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256,
                    HashSize);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
