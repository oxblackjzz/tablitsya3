using System;

namespace Tablitsya3.Models
{
    /// <summary>
    /// Конфігурація окремого цеху з усіма параметрами планування
    /// </summary>
    public class WorkshopConfig
    {
        /// <summary>
        /// Унікальний номер цеху
        /// </summary>
        public int Number { get; set; }
        
        /// <summary>
        /// Назва цеху для відображення
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Потужність виробництва (м²/день)
        /// </summary>
        public int Capacity { get; set; } = 1000;
        
        /// <summary>
        /// Використовувати автоматичний розрахунок потужності від станцій
        /// </summary>
        public bool UseAutoCapacity { get; set; } = false;
        
        /// <summary>
        /// Тривалість виробництва (днів)
        /// </summary>
        public int ProductionLeadTime { get; set; } = 5;
        
        /// <summary>
        /// Днів до початку виробництва (підготовка)
        /// </summary>
        public int DaysBeforeProduction { get; set; } = 16;
        
        /// <summary>
        /// Колір для відображення в UI (CSS клас Bootstrap)
        /// </summary>
        public string ColorClass { get; set; } = "primary";
        
        /// <summary>
        /// Чи активний цех
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Порядок сортування для відображення
        /// </summary>
        public int SortOrder { get; set; }
        
        /// <summary>
        /// Дата створення
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Створити конфігурацію цеху за замовчуванням
        /// </summary>
        public static WorkshopConfig CreateDefault(int number)
        {
            var colors = new[] { "primary", "success", "warning", "info", "danger", "secondary" };
            return new WorkshopConfig
            {
                Number = number,
                Name = $"Цех №{number}",
                Capacity = 1000,
                ProductionLeadTime = 5,
                DaysBeforeProduction = 16,
                ColorClass = colors[(number - 1) % colors.Length],
                SortOrder = number,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
        }
    }
}
