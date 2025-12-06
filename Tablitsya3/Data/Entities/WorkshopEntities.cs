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

        [Column("production_lead_time")]
        public int ProductionLeadTime { get; set; }

        [Column("days_before_production")]
        public int DaysBeforeProduction { get; set; }

        // ❌ ВИДАЛЕНО Navigation properties - таблиці незалежні
        // Не потрібні для нашої схеми БД без foreign keys
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
}
