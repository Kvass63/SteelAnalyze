namespace SteelAnalyze.Auth
{
    // Учебное хранилище пользователей в памяти
    public class UserStore
    {
        private readonly List<AppUser> _users =
        [
            new AppUser("admin", "admin123", Roles.Admin),
            new AppUser("operator", "operator123", Roles.User)
        ];

        public AppUser? Validate(string username, string password)
        {
            var user = _users.FirstOrDefault(u => u.Username == username);
            return user != null && user.VerifyPassword(password) ? user : null;
        }
    }
}
