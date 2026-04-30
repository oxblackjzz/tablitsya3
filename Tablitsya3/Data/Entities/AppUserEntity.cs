using System.ComponentModel.DataAnnotations;

namespace Tablitsya3.Data.Entities
{
    public class AppUserEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string PasswordSalt { get; set; } = string.Empty;

        [MaxLength(128)]
        public string DisplayName { get; set; } = string.Empty;

        public int Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }
    }
}
