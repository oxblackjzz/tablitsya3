namespace Tablitsya3.Models.Scanning
{
    /// <summary>
    /// Імпортований проект з .project файлу
    /// </summary>
    public class ImportedProject
    {
        public int Id { get; set; }
        
        /// <summary>UUID проекту з XML</summary>
        public string ProjectUuid { get; set; } = string.Empty;
        
        /// <summary>Назва файлу</summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>Дата імпорту</summary>
        public DateTime ImportedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>Загальна вартість</summary>
        public decimal TotalCost { get; set; }
        
        /// <summary>Вартість матеріалів</summary>
        public decimal MaterialCost { get; set; }
        
        /// <summary>Вартість операцій</summary>
        public decimal OperationCost { get; set; }
        
        /// <summary>Валюта</summary>
        public string Currency { get; set; } = "грн";
        
        /// <summary>Версія файлу</summary>
        public string Version { get; set; } = string.Empty;
        
        /// <summary>Кількість товарів (goods)</summary>
        public int ProductsCount { get; set; }
        
        /// <summary>Кількість деталей (parts)</summary>
        public int PartsCount { get; set; }
        
        /// <summary>Загальна площа в м²</summary>
        public double TotalSquareMeters { get; set; }
        
        /// <summary>Товари з проекту</summary>
        public List<ImportedProduct> Products { get; set; } = new();
    }

    /// <summary>
    /// Товар (good) з .project файлу
    /// </summary>
    public class ImportedProduct
    {
        public int Id { get; set; }
        
        /// <summary>ID товару в XML</summary>
        public int ProductId { get; set; }
        
        /// <summary>Назва товару (замовлення)</summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>Код товару</summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>Опис (розміри)</summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>Кількість</summary>
        public int Count { get; set; } = 1;
        
        /// <summary>Вартість одиниці</summary>
        public decimal Cost { get; set; }
        
        /// <summary>Вартість матеріалів</summary>
        public decimal MaterialCost { get; set; }
        
        /// <summary>Вартість операцій</summary>
        public decimal OperationCost { get; set; }
        
        /// <summary>Дата замовлення (з XML)</summary>
        public DateTime? OrderDate { get; set; }
        
        /// <summary>Деталі товару</summary>
        public List<Part> Parts { get; set; } = new();
        
        /// <summary>Загальна площа в м²</summary>
        public double TotalSquareMeters => Parts.Sum(p => p.SquareMeters * p.Quantity);
        
        /// <summary>Прогрес виконання (0-100%)</summary>
        public int ProgressPercent
        {
            get
            {
                if (Parts.Count == 0) return 0;
                var totalProgress = Parts.Sum(p => p.ProgressPercent);
                return (int)Math.Round((double)totalProgress / Parts.Count);
            }
        }
        
        /// <summary>Кількість завершених деталей</summary>
        public int CompletedPartsCount => Parts.Count(p => p.IsFullyCompleted);
    }

    /// <summary>
    /// Результат сканування
    /// </summary>
    public class ScanResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Part? Part { get; set; }
        public ProductionStage? Stage { get; set; }
        public bool IsFullyCompleted { get; set; }
        
        /// <summary>Ім'я працівника, що виконав сканування</summary>
        public string? WorkerName { get; set; }
        
        /// <summary>Назва робочої станції</summary>
        public string? WorkstationName { get; set; }
    }

    /// <summary>
    /// Результат імпорту проекту
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AddedCount { get; set; }
        public int SkippedCount { get; set; }
        public int TotalCount { get; set; }
        public ImportedProject? Project { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Статистика по проекту
    /// </summary>
    public class ProjectStatistics
    {
        public string ProjectUuid { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int TotalParts { get; set; }
        public int CompletedParts { get; set; }
        public double TotalSquareMeters { get; set; }
        public double CompletedSquareMeters { get; set; }
        
        public Dictionary<ProductionStage, StageStatistics> StageStats { get; set; } = new();
        
        public int ProgressPercent => TotalParts > 0 
            ? (int)Math.Round((double)CompletedParts / TotalParts * 100) 
            : 0;
    }

    /// <summary>
    /// Статистика по етапу
    /// </summary>
    public class StageStatistics
    {
        public ProductionStage Stage { get; set; }
        public int TotalRequired { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        
        public int ProgressPercent => TotalRequired > 0 
            ? (int)Math.Round((double)Completed / TotalRequired * 100) 
            : 0;
    }
}
