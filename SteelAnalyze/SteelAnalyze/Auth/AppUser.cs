using System.Security.Cryptography;

namespace SteelAnalyze.Auth
{
    public class AppUser
    {
        private const int Iterations = 100_000;

        public string Username { get; }
        public string Role { get; }
        private readonly byte[] _salt;
        private readonly byte[] _hash;

        public AppUser(string username, string password, string role)
        {
            Username = username;
            Role = role;
            _salt = RandomNumberGenerator.GetBytes(16);
            _hash = Hash(password, _salt);
        }

        // Бизнес-логика: проверка пароля без хранения его в открытом виде
        public bool VerifyPassword(string password) =>
            CryptographicOperations.FixedTimeEquals(_hash, Hash(password, _salt));

        private static byte[] Hash(string password, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
    }
}
