using ElectricalBilling.Models;

namespace ElectricalBilling.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Validates credentials against the stored (hashed) password.
        /// Returns the User if valid and active, otherwise null.
        /// </summary>
        Task<User?> ValidateCredentialsAsync(string usernameOrEmail, string password);

        /// <summary>
        /// Creates a new user with a securely hashed password.
        /// Used only for initial admin seeding / future user management.
        /// </summary>
        Task<User> CreateUserAsync(string fullName, string username, string email, string plainPassword);

        Task UpdateLastLoginAsync(int userId);
    }
}
