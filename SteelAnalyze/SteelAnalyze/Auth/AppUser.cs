using System.Security.Cryptography;

namespace SteelAnalyze.Auth
{
    public class AppUser
    {
        private const int Iterations = 100_000;

        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = Roles.User;
        public byte[] PasswordSalt { get; set; } = [];
        public byte[] PasswordHash { get; set; } = [];

        public static AppUser Create(string username, string password, string role)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            return new AppUser
            {
                Username = username,
                Role = role,
                PasswordSalt = salt,
                PasswordHash = Hash(password, salt)
            };
        }

        // Бизнес-логика: проверка пароля без хранения его в открытом виде
        public bool VerifyPassword(string password) =>
            CryptographicOperations.FixedTimeEquals(PasswordHash, Hash(password, PasswordSalt));

        private static byte[] Hash(string password, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
    }
}
