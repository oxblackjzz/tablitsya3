using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tablitsya3.Data.Entities
{
    [Table("workshop_data")]
    public class WorkshopDataEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Obsolete("Використовуйте WorkshopProductionLeadTimeEntity")]
        [Column("production_lead_time")]
        public int ProductionLeadTime { get; set; }

        [Obsolete("Використовуйте WorkshopDaysBeforeProductionEntity")]
        [Column("days_before_production")]
        public int DaysBeforeProduction { get; set; }
    }

    [Table("orders")]
    public class OrderEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("workshop_number")]
        public int WorkshopNumber { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; }

        [Column("square_meters")]
        public double SquareMeters { get; set; }

        [Column("order_name")]
        [MaxLength(500)]
        public string OrderName { get; set; } = string.Empty;
    }

    [Table("workshop_capacities")]
    public class WorkshopCapacityEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("workshop_number")]
        public int WorkshopNumber { get; set; }

        [Column("capacity")]
        public int Capacity { get; set; }
    }

    [Table("workshop_production_lead_times")]
    public class WorkshopProductionLeadTimeEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("workshop_number")]
        public int WorkshopNumber { get; set; }

        [Column("production_lead_time")]
        public int ProductionLeadTime { get; set; }
    }

    [Table("workshop_days_before_production")]
    public class WorkshopDaysBeforeProductionEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("workshop_number")]
        public int WorkshopNumber { get; set; }

        [Column("days_before_production")]
        public int DaysBeforeProduction { get; set; }
    }

    [Table("custom_completion_dates")]
    public class CustomCompletionDateEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("order_key")]
        [MaxLength(200)]
        public string OrderKey { get; set; } = string.Empty;

        [Column("completion_date")]
        public DateTime CompletionDate { get; set; }
    }
    
    /// <summary>
    /// Зберігає оригінальний цех для переміщених замовлень (для збереження кольору)
    /// </summary>
    [Table("original_workshops")]
    public class OriginalWorkshopEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Ключ - назва замовлення (OrderName)
        /// </summary>
        [Column("order_key")]
        [MaxLength(200)]
        public string OrderKey { get; set; } = string.Empty;

        /// <summary>
        /// Номер оригінального цеху (звідки було переміщено)
        /// </summary>
        [Column("original_workshop_number")]
        public int OriginalWorkshopNumber { get; set; }
    }
}
