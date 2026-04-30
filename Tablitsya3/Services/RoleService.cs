using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для керування динамічними ролями, їх дозволами та per-user overrides.
    /// </summary>
    public class RoleService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RoleService> _logger;

        public RoleService(ApplicationDbContext db, ILogger<RoleService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // === Ролі ===

        public Task<List<RoleEntity>> GetAllRolesAsync()
            => _db.Roles.OrderByDescending(r => r.IsSystem).ThenBy(r => r.Code).ToListAsync();

        public Task<RoleEntity?> GetRoleAsync(int id) => _db.Roles.FirstOrDefaultAsync(r => r.Id == id);

        public Task<RoleEntity?> GetRoleByCodeAsync(string code)
            => _db.Roles.FirstOrDefaultAsync(r => r.Code == code);

        public async Task<RoleEntity> CreateRoleAsync(string code, string displayName, string? description, string? badgeColor)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Введіть код ролі");
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Введіть назву ролі");

            code = code.Trim();
            if (await _db.Roles.AnyAsync(r => r.Code == code))
                throw new InvalidOperationException($"Роль '{code}' вже існує");

            var role = new RoleEntity
            {
                Code = code,
                DisplayName = displayName.Trim(),
                Description = description?.Trim(),
                BadgeColor = string.IsNullOrWhiteSpace(badgeColor) ? "#6c757d" : badgeColor.Trim(),
                IsSystem = false,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Created role {Code}", code);
            return role;
        }

        public async Task UpdateRoleAsync(int id, string displayName, string? description, string? badgeColor)
        {
            var role = await _db.Roles.FindAsync(id) ?? throw new InvalidOperationException("Роль не знайдена");
            role.DisplayName = displayName?.Trim() ?? role.DisplayName;
            role.Description = description?.Trim();
            if (!string.IsNullOrWhiteSpace(badgeColor)) role.BadgeColor = badgeColor.Trim();
            role.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteRoleAsync(int id)
        {
            var role = await _db.Roles.FindAsync(id) ?? throw new InvalidOperationException("Роль не знайдена");
            if (role.IsSystem) throw new InvalidOperationException("Системну роль не можна видалити");

            var inUse = await _db.AppUsers.AnyAsync(u => u.RoleId == id);
            if (inUse) throw new InvalidOperationException("Роль використовується користувачами — спочатку перепризначте їх");

            var perms = _db.RolePermissions.Where(p => p.RoleId == id);
            _db.RolePermissions.RemoveRange(perms);
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
        }

        // === Permissions ролі ===

        public async Task<HashSet<string>> GetRolePermissionsAsync(int roleId)
        {
            var keys = await _db.RolePermissions
                .Where(p => p.RoleId == roleId)
                .Select(p => p.PermissionKey)
                .ToListAsync();
            return new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        }

        public async Task SetRolePermissionsAsync(int roleId, IEnumerable<string> permissionKeys)
        {
            var role = await _db.Roles.FindAsync(roleId) ?? throw new InvalidOperationException("Роль не знайдена");

            var desired = new HashSet<string>(
                permissionKeys.Where(Permissions.IsKnownKey),
                StringComparer.OrdinalIgnoreCase);

            var existing = await _db.RolePermissions.Where(p => p.RoleId == roleId).ToListAsync();

            foreach (var ex in existing)
                if (!desired.Contains(ex.PermissionKey))
                    _db.RolePermissions.Remove(ex);

            var existingKeys = new HashSet<string>(existing.Select(e => e.PermissionKey), StringComparer.OrdinalIgnoreCase);
            foreach (var key in desired)
                if (!existingKeys.Contains(key))
                    _db.RolePermissions.Add(new RolePermissionEntity { RoleId = roleId, PermissionKey = key });

            role.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // === Per-user overrides ===

        public async Task<List<UserPermissionOverrideEntity>> GetUserOverridesAsync(int userId)
            => await _db.UserPermissionOverrides.Where(o => o.UserId == userId).ToListAsync();

        public async Task SetUserOverridesAsync(int userId, IDictionary<string, bool> overrides)
        {
            var existing = await _db.UserPermissionOverrides.Where(o => o.UserId == userId).ToListAsync();

            foreach (var ex in existing)
            {
                if (!overrides.ContainsKey(ex.PermissionKey))
                {
                    _db.UserPermissionOverrides.Remove(ex);
                }
            }

            var existingMap = existing.ToDictionary(e => e.PermissionKey, StringComparer.OrdinalIgnoreCase);

            foreach (var kv in overrides)
            {
                if (!Permissions.IsKnownKey(kv.Key)) continue;

                if (existingMap.TryGetValue(kv.Key, out var found))
                {
                    if (found.IsGranted != kv.Value)
                        found.IsGranted = kv.Value;
                }
                else
                {
                    _db.UserPermissionOverrides.Add(new UserPermissionOverrideEntity
                    {
                        UserId = userId,
                        PermissionKey = kv.Key,
                        IsGranted = kv.Value,
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task ClearUserOverridesAsync(int userId)
        {
            var existing = _db.UserPermissionOverrides.Where(o => o.UserId == userId);
            _db.UserPermissionOverrides.RemoveRange(existing);
            await _db.SaveChangesAsync();
        }

        // === Обчислення ефективних прав ===

        /// <summary>
        /// Ефективні дозволи користувача: (Role permissions) + (overrides grant) - (overrides deny).
        /// </summary>
        public async Task<HashSet<string>> GetEffectivePermissionsAsync(int userId)
        {
            var user = await _db.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (user.RoleId.HasValue)
            {
                var rolePerms = await _db.RolePermissions
                    .Where(p => p.RoleId == user.RoleId.Value)
                    .Select(p => p.PermissionKey)
                    .ToListAsync();
                foreach (var p in rolePerms) result.Add(p);
            }
            else
            {
                // Fallback на legacy enum-Role
                var legacy = (UserRole)user.Role;
                var code = legacy.ToString();
                if (Permissions.DefaultRolePermissions.TryGetValue(code, out var defaults))
                    foreach (var p in defaults) result.Add(p);
            }

            var overrides = await _db.UserPermissionOverrides
                .Where(o => o.UserId == userId)
                .ToListAsync();

            foreach (var o in overrides)
            {
                if (o.IsGranted) result.Add(o.PermissionKey);
                else result.Remove(o.PermissionKey);
            }

            return result;
        }
    }
}
