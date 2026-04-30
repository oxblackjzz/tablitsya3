using System.ComponentModel.DataAnnotations;

namespace Tablitsya3.Data.Entities
{
    /// <summary>
    /// Роль / тип користувача системи. Адмін може створювати/редагувати/видаляти.
    /// </summary>
    public class RoleEntity
    {
        public int Id { get; set; }

        /// <summary>Системний код ролі (Admin/Manager/...). Унікальний.</summary>
        [Required]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        /// <summary>Відображувана назва</summary>
        [Required]
        [MaxLength(128)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>HEX-колір бейджа (#dc3545 і т.д.)</summary>
        [MaxLength(16)]
        public string? BadgeColor { get; set; }

        /// <summary>Системна роль (Admin) — не можна видалити чи перейменувати код</summary>
        public bool IsSystem { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Permission-ключ, дозволений для ролі (orders.view, orders.edit і т.д.)
    /// </summary>
    public class RolePermissionEntity
    {
        public int Id { get; set; }
        public int RoleId { get; set; }

        [Required]
        [MaxLength(128)]
        public string PermissionKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Override прав на конкретного користувача (поверх ролі).
    /// IsGranted = true: явно надати; false: явно заборонити (deny має пріоритет над role).
    /// </summary>
    public class UserPermissionOverrideEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        [MaxLength(128)]
        public string PermissionKey { get; set; } = string.Empty;

        public bool IsGranted { get; set; }
    }
}
