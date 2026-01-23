using Tablitsya3.Data.Entities;

namespace Tablitsya3.Models.Scanning
{
    /// <summary>
    /// Статистика роботи працівника
    /// </summary>
    public class WorkerStatistics
    {
        public int WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public string WorkerCode { get; set; } = string.Empty;
        public int WorkshopNumber { get; set; }
        
        /// <summary>Статистика за сьогодні</summary>
        public PeriodStatistics Today { get; set; } = new();
        
        /// <summary>Статистика за тиждень</summary>
        public PeriodStatistics Week { get; set; } = new();
        
        /// <summary>Статистика за місяць</summary>
        public PeriodStatistics Month { get; set; } = new();
        
        /// <summary>Статистика за весь час</summary>
        public PeriodStatistics AllTime { get; set; } = new();
        
        /// <summary>Статистика по етапах</summary>
        public Dictionary<ProductionStage, StageWorkerStatistics> StageStats { get; set; } = new();
        
        /// <summary>Статистика по днях (для графіка)</summary>
        public List<DailyStatistics> DailyStats { get; set; } = new();
    }

    /// <summary>
    /// Статистика за період
    /// </summary>
    public class PeriodStatistics
    {
        /// <summary>Кількість оброблених деталей</summary>
        public int PartsProcessed { get; set; }
        
        /// <summary>Загальна площа в м²</summary>
        public double TotalSquareMeters { get; set; }
        
        /// <summary>Кількість успішних сканувань</summary>
        public int SuccessfulScans { get; set; }
        
        /// <summary>Кількість помилкових сканувань</summary>
        public int FailedScans { get; set; }
        
        /// <summary>Середній час між скануваннями (секунди)</summary>
        public double AvgTimeBetweenScans { get; set; }
        
        /// <summary>Форматована площа</summary>
        public string FormattedSquareMeters => $"{TotalSquareMeters:F2} м²";
    }

    /// <summary>
    /// Статистика працівника по етапу
    /// </summary>
    public class StageWorkerStatistics
    {
        public ProductionStage Stage { get; set; }
        public int PartsProcessed { get; set; }
        public double TotalSquareMeters { get; set; }
        public int SuccessfulScans { get; set; }
    }

    /// <summary>
    /// Денна статистика
    /// </summary>
    public class DailyStatistics
    {
        public DateTime Date { get; set; }
        public int PartsProcessed { get; set; }
        public double TotalSquareMeters { get; set; }
        public int ScansCount { get; set; }
    }

    /// <summary>
    /// Загальна статистика по всіх працівниках
    /// </summary>
    public class WorkersOverallStatistics
    {
        public int TotalWorkers { get; set; }
        public int ActiveWorkersToday { get; set; }
        public double TotalSquareMetersToday { get; set; }
        public double TotalSquareMetersWeek { get; set; }
        public double TotalSquareMetersMonth { get; set; }
        public int TotalPartsToday { get; set; }
        public int TotalPartsWeek { get; set; }
        public int TotalPartsMonth { get; set; }
        
        /// <summary>Топ працівників за сьогодні</summary>
        public List<WorkerRanking> TopWorkersToday { get; set; } = new();
        
        /// <summary>Топ працівників за тиждень</summary>
        public List<WorkerRanking> TopWorkersWeek { get; set; } = new();
        
        /// <summary>Топ працівників за місяць</summary>
        public List<WorkerRanking> TopWorkersMonth { get; set; } = new();
    }

    /// <summary>
    /// Рейтинг працівника
    /// </summary>
    public class WorkerRanking
    {
        public int WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public int WorkshopNumber { get; set; }
        public int PartsProcessed { get; set; }
        public double TotalSquareMeters { get; set; }
        public int Rank { get; set; }
    }
}
