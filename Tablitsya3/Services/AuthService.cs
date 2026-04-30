using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс автентифікації користувачів і керування обліковими записами
    /// </summary>
    public class AuthService
    {
        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private const int Iterations = 100_000;

        private readonly ApplicationDbContext _db;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext db, ILogger<AuthService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // === Hashing ===

        public static (string Hash, string Salt) HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Пароль не може бути порожнім", nameof(password));

            var saltBytes = RandomNumberGenerator.GetBytes(SaltBytes);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashBytes);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        public static bool VerifyPassword(string password, string hash, string salt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
                return false;

            try
            {
                var saltBytes = Convert.FromBase64String(salt);
                var expected = Convert.FromBase64String(hash);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch
            {
                return false;
            }
        }

        // === Authentication ===

        public async Task<AppUserEntity?> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return null;

            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !user.IsActive)
                return null;

            if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                return null;

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return user;
        }

        // === CRUD ===

        public async Task<List<AppUserEntity>> GetAllAsync()
        {
            return await _db.AppUsers.OrderBy(u => u.Username).ToListAsync();
        }

        public async Task<AppUserEntity?> GetByIdAsync(int id)
        {
            return await _db.AppUsers.FindAsync(id);
        }

        public async Task<AppUserEntity?> GetByUsernameAsync(string username)
        {
            return await _db.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<AppUserEntity> CreateAsync(string username, string password, string displayName, UserRole role, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Введіть логін");
            if (string.IsNullOrEmpty(password) || password.Length < 4)
                throw new ArgumentException("Пароль має містити щонайменше 4 символи");

            username = username.Trim().ToLowerInvariant();

            var existing = await GetByUsernameAsync(username);
            if (existing != null)
                throw new InvalidOperationException($"Користувач '{username}' вже існує");

            var (hash, salt) = HashPassword(password);
            var user = new AppUserEntity
            {
                Username = username,
                PasswordHash = hash,
                PasswordSalt = salt,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName.Trim(),
                Role = (int)role,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
            };
            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Created user {Username} with role {Role}", username, role);
            return user;
        }

        public async Task UpdateAsync(int id, string displayName, UserRole role, bool isActive)
        {
            var user = await _db.AppUsers.FindAsync(id) ?? throw new InvalidOperationException("Користувача не знайдено");
            user.DisplayName = displayName?.Trim() ?? user.Username;
            user.Role = (int)role;
            user.IsActive = isActive;
            await _db.SaveChangesAsync();
        }

        public async Task SetPasswordAsync(int id, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 4)
                throw new ArgumentException("Пароль має містити щонайменше 4 символи");

            var user = await _db.AppUsers.FindAsync(id) ?? throw new InvalidOperationException("Користувача не знайдено");
            var (hash, salt) = HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _db.AppUsers.FindAsync(id);
            if (user == null) return;
            _db.AppUsers.Remove(user);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> AnyUserExistsAsync()
        {
            return await _db.AppUsers.AnyAsync();
        }
    }
}
