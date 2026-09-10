using ElectricalBilling.Data;
using ElectricalBilling.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElectricalBilling.Services
{
    /// <summary>
    /// Handles credential validation and user creation.
    /// Uses Microsoft.AspNetCore.Identity's PasswordHasher, which implements
    /// PBKDF2 with a per-password salt. This does NOT require the full
    /// ASP.NET Core Identity system - just the hasher, which is lightweight
    /// and appropriate for a single-business installation.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<User?> ValidateCredentialsAsync(string usernameOrEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var normalizedInput = usernameOrEmail.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.IsActive &&
                    (u.Username.ToLower() == normalizedInput || u.Email.ToLower() == normalizedInput));

            if (user is null)
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            return result == PasswordVerificationResult.Success ||
                   result == PasswordVerificationResult.SuccessRehashNeeded
                ? user
                : null;
        }

        public async Task<User> CreateUserAsync(string fullName, string username, string email, string plainPassword)
        {
            var user = new User
            {
                FullName = fullName,
                Username = username,
                Email = email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, plainPassword);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user is not null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
