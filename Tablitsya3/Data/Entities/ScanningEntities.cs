using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tablitsya3.Data.Entities
{
    /// <summary>
    /// Entity для імпортованого проекту
    /// </summary>
    public class ImportedProjectEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProjectUuid { get; set; } = string.Empty;

        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        public DateTime ImportedDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaterialCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OperationCost { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "грн";

        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;

        public int ProductsCount { get; set; }
        public int PartsCount { get; set; }
        public double TotalSquareMeters { get; set; }

        /// <summary>Чи активний проект</summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>Номер цеху</summary>
        public int WorkshopNumber { get; set; } = 1;
    }

    /// <summary>
    /// Entity для деталі
    /// </summary>
    public class PartEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProjectExternalUuid { get; set; } = string.Empty;

        public int PartId { get; set; }
        public int PartCounter { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        public double Length { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; } = 16;

        [MaxLength(100)]
        public string Material { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string SourceFileName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string OrderName { get; set; } = string.Empty;

        // === Статуси етапів ===
        public bool IsCutCompleted { get; set; }
        public DateTime? CutCompletedDate { get; set; }

        public bool IsEdgeBandingCompleted { get; set; }
        public DateTime? EdgeBandingCompletedDate { get; set; }

        public bool IsDrillingCompleted { get; set; }
        public DateTime? DrillingCompletedDate { get; set; }

        public bool IsSortingCompleted { get; set; }
        public DateTime? SortingCompletedDate { get; set; }

        public bool IsPackingCompleted { get; set; }
        public DateTime? PackingCompletedDate { get; set; }

        // === Вимоги до етапів ===
        public bool RequiresCutting { get; set; } = true;
        public bool RequiresEdgeBanding { get; set; } = true;
        public bool RequiresDrilling { get; set; } = true;
        public bool RequiresSorting { get; set; } = true;
        public bool RequiresPacking { get; set; } = true;

        // === Обклейка кромкою ===
        public int EdgeBandingSidesRequired { get; set; } = 0;
        public int EdgeBandingSidesCompleted { get; set; } = 0;

        [MaxLength(500)]
        public string EdgeBandingCompletedDates { get; set; } = string.Empty;

        // === Обчислювані поля (не зберігаються) ===
        [NotMapped]
        public double SquareMeters => (Length * Width) / 1_000_000;

        [NotMapped]
        public string QRCode => $"{ProjectExternalUuid}/{PartId}/{PartCounter}";

        [NotMapped]
        public string Dimensions => $"{Length:F0}x{Width:F0}";
    }

    /// <summary>
    /// Entity для логу сканувань
    /// </summary>
    public class ScanLogEntity
    {
        [Key]
        public int Id { get; set; }

        public int PartId { get; set; }

        [Required]
        [MaxLength(255)]
        public string QRCode { get; set; } = string.Empty;

        public int Stage { get; set; } // ProductionStage enum value

        public DateTime ScanDate { get; set; } = DateTime.UtcNow;

        /// <summary>ID працівника (застаріле, використовуйте WorkerId)</summary>
        [MaxLength(100)]
        public string? UserId { get; set; }

        /// <summary>ID працівника з таблиці workers</summary>
        public int? WorkerId { get; set; }

        /// <summary>ID робочої станції</summary>
        public int? WorkstationId { get; set; }

        [MaxLength(50)]
        public string? DeviceId { get; set; }

        public bool Success { get; set; } = true;

        [MaxLength(500)]
        public string? Message { get; set; }

        /// <summary>ID сесії працівника</summary>
        public int? SessionId { get; set; }
    }

    /// <summary>
    /// Entity для товару в проекті
    /// </summary>
    public class ProductEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProjectUuid { get; set; } = string.Empty;

        public int ProductId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        public int Count { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaterialCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OperationCost { get; set; }

        public DateTime? OrderDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
