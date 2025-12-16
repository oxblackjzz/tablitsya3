using System;

namespace Tablitsya3.Models
{
    /// <summary>
    /// Дані замовлення з унікальним ідентифікатором
    /// </summary>
    public class OrderData
    {
        /// <summary>
        /// Унікальний ідентифікатор замовлення
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Площа замовлення в м²
        /// </summary>
        public double SquareMeters { get; set; }
        
        /// <summary>
        /// Дата надходження замовлення
        /// </summary>
        public DateTime OrderDate { get; set; } = DateTime.Today;
        
        /// <summary>
        /// Назва/номер замовлення
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Кастомна дата відвантаження (якщо задана)
        /// </summary>
        public DateTime? CustomCompletionDate { get; set; }
        
        /// <summary>
        /// Номер цеху (оригінальний, до переміщення)
        /// </summary>
        public int? OriginalWorkshopNumber { get; set; }
        
        /// <summary>
        /// Дата створення запису
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Дата останньої зміни
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Створює копію замовлення
        /// </summary>
        public OrderData Clone()
        {
            return new OrderData
            {
                Id = this.Id,
                SquareMeters = this.SquareMeters,
                OrderDate = this.OrderDate,
                Name = this.Name,
                CustomCompletionDate = this.CustomCompletionDate,
                OriginalWorkshopNumber = this.OriginalWorkshopNumber,
                CreatedAt = this.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Створює нове замовлення з новим Id
        /// </summary>
        public OrderData CloneWithNewId()
        {
            var clone = Clone();
            clone.Id = Guid.NewGuid();
            clone.CreatedAt = DateTime.UtcNow;
            return clone;
        }
        
        public override string ToString()
        {
            return $"{Name} ({SquareMeters:N0} м²)";
        }
    }
}
