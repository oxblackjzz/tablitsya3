using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data;
using Tablitsya3.Models.Scanning;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для отримання прогресу сканування по замовленням
    /// </summary>
    public class ScanningProgressService
    {
        private readonly ApplicationDbContext _context;
        private readonly LoggingService _logger;

        public ScanningProgressService(ApplicationDbContext context, LoggingService logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Отримати прогрес сканування для замовлення за назвою
        /// </summary>
        public async Task<OrderScanProgress?> GetOrderProgressAsync(string orderName)
        {
            if (string.IsNullOrEmpty(orderName)) return null;

            try
            {
                var parts = await _context.Parts
                    .Where(p => p.OrderName == orderName || p.OrderName.Contains(orderName))
                    .ToListAsync();

                if (!parts.Any()) return null;

                return CalculateProgress(orderName, parts);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка отримання прогресу для {orderName}: {ex.Message}", ex, "ScanningProgress");
                return null;
            }
        }

        /// <summary>
        /// Отримати прогрес для декількох замовлень одночасно (оптимізовано)
        /// </summary>
        public async Task<Dictionary<string, OrderScanProgress>> GetOrdersProgressAsync(IEnumerable<string> orderNames)
        {
            var result = new Dictionary<string, OrderScanProgress>();
            var validNames = orderNames.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
            
            if (!validNames.Any()) return result;

            try
            {
                // Завантажуємо всі деталі які можуть відповідати замовленням
                var allParts = await _context.Parts
                    .Where(p => validNames.Any(name => p.OrderName == name || p.OrderName.Contains(name)))
                    .ToListAsync();

                // Групуємо по OrderName
                var groupedParts = allParts.GroupBy(p => p.OrderName).ToDictionary(g => g.Key, g => g.ToList());

                // Обчислюємо прогрес для кожного замовлення
                foreach (var orderName in validNames)
                {
                    // Шукаємо точний збіг або часткове входження
                    var matchingParts = groupedParts
                        .Where(kvp => kvp.Key == orderName || kvp.Key.Contains(orderName) || orderName.Contains(kvp.Key))
                        .SelectMany(kvp => kvp.Value)
                        .ToList();

                    if (matchingParts.Any())
                    {
                        result[orderName] = CalculateProgress(orderName, matchingParts);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка отримання прогресу замовлень: {ex.Message}", ex, "ScanningProgress");
                return result;
            }
        }

        /// <summary>
        /// Отримати загальну статистику для всіх замовлень
        /// </summary>
        public async Task<OverallScanProgress> GetOverallProgressAsync()
        {
            try
            {
                var parts = await _context.Parts.ToListAsync();

                return new OverallScanProgress
                {
                    TotalParts = parts.Count,
                    CompletedParts = parts.Count(p => p.IsPackingCompleted),
                    TotalSquareMeters = parts.Sum(p => p.SquareMeters),
                    CompletedSquareMeters = parts.Where(p => p.IsPackingCompleted).Sum(p => p.SquareMeters),
                    CuttingCompleted = parts.Count(p => p.IsCutCompleted),
                    EdgeBandingCompleted = parts.Count(p => p.IsEdgeBandingCompleted),
                    DrillingCompleted = parts.Count(p => p.IsDrillingCompleted),
                    SortingCompleted = parts.Count(p => p.IsSortingCompleted),
                    PackingCompleted = parts.Count(p => p.IsPackingCompleted)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка отримання загальної статистики: {ex.Message}", ex, "ScanningProgress");
                return new OverallScanProgress();
            }
        }

        private OrderScanProgress CalculateProgress(string orderName, List<Data.Entities.PartEntity> parts)
        {
            var totalParts = parts.Count;
            var completedParts = parts.Count(p => p.IsPackingCompleted);
            
            // Обчислюємо прогрес по етапах
            var stageProgress = new Dictionary<ProductionStage, StageProgress>();
            
            foreach (ProductionStage stage in Enum.GetValues<ProductionStage>())
            {
                var required = parts.Count(p => IsStageRequired(p, stage));
                var completed = parts.Count(p => IsStageCompleted(p, stage));
                
                stageProgress[stage] = new StageProgress
                {
                    Stage = stage,
                    TotalRequired = required,
                    Completed = completed,
                    Percent = required > 0 ? (int)Math.Round((double)completed / required * 100) : 100
                };
            }

            return new OrderScanProgress
            {
                OrderName = orderName,
                TotalParts = totalParts,
                CompletedParts = completedParts,
                ProgressPercent = totalParts > 0 ? (int)Math.Round((double)completedParts / totalParts * 100) : 0,
                TotalSquareMeters = parts.Sum(p => p.SquareMeters),
                CompletedSquareMeters = parts.Where(p => p.IsPackingCompleted).Sum(p => p.SquareMeters),
                StageProgress = stageProgress,
                CurrentStage = DetermineCurrentStage(stageProgress),
                Status = DetermineStatus(totalParts, completedParts, stageProgress)
            };
        }

        private bool IsStageRequired(Data.Entities.PartEntity entity, ProductionStage stage)
        {
            return stage switch
            {
                ProductionStage.Cutting => entity.RequiresCutting,
                ProductionStage.EdgeBanding => entity.RequiresEdgeBanding,
                ProductionStage.Drilling => entity.RequiresDrilling,
                ProductionStage.Sorting => entity.RequiresSorting,
                ProductionStage.Packing => entity.RequiresPacking,
                _ => false
            };
        }

        private bool IsStageCompleted(Data.Entities.PartEntity entity, ProductionStage stage)
        {
            return stage switch
            {
                ProductionStage.Cutting => entity.IsCutCompleted,
                ProductionStage.EdgeBanding => entity.EdgeBandingSidesRequired > 0 
                    ? entity.EdgeBandingSidesCompleted >= entity.EdgeBandingSidesRequired 
                    : entity.IsEdgeBandingCompleted,
                ProductionStage.Drilling => entity.IsDrillingCompleted,
                ProductionStage.Sorting => entity.IsSortingCompleted,
                ProductionStage.Packing => entity.IsPackingCompleted,
                _ => false
            };
        }

        private ProductionStage? DetermineCurrentStage(Dictionary<ProductionStage, StageProgress> stageProgress)
        {
            foreach (ProductionStage stage in Enum.GetValues<ProductionStage>())
            {
                if (stageProgress.TryGetValue(stage, out var progress))
                {
                    if (progress.TotalRequired > 0 && progress.Completed < progress.TotalRequired)
                    {
                        return stage;
                    }
                }
            }
            return null;
        }

        private OrderScanStatus DetermineStatus(int totalParts, int completedParts, Dictionary<ProductionStage, StageProgress> stageProgress)
        {
            if (totalParts == 0) return OrderScanStatus.NotStarted;
            if (completedParts == totalParts) return OrderScanStatus.Completed;
            
            // Перевіряємо чи почалась порізка
            if (stageProgress.TryGetValue(ProductionStage.Cutting, out var cuttingProgress))
            {
                if (cuttingProgress.Completed == 0) return OrderScanStatus.NotStarted;
            }
            
            return OrderScanStatus.InProgress;
        }
    }

    /// <summary>
    /// Прогрес сканування для одного замовлення
    /// </summary>
    public class OrderScanProgress
    {
        public string OrderName { get; set; } = string.Empty;
        public int TotalParts { get; set; }
        public int CompletedParts { get; set; }
        public int ProgressPercent { get; set; }
        public double TotalSquareMeters { get; set; }
        public double CompletedSquareMeters { get; set; }
        public Dictionary<ProductionStage, StageProgress> StageProgress { get; set; } = new();
        public ProductionStage? CurrentStage { get; set; }
        public OrderScanStatus Status { get; set; }
        
        /// <summary>
        /// Колір прогресу для відображення на діаграмі
        /// </summary>
        public string ProgressColor => Status switch
        {
            OrderScanStatus.Completed => "#28a745",      // Зелений
            OrderScanStatus.InProgress => "#17a2b8",     // Синій
            OrderScanStatus.NotStarted => "#6c757d",     // Сірий
            _ => "#6c757d"
        };
        
        /// <summary>
        /// Іконка для статусу
        /// </summary>
        public string StatusIcon => Status switch
        {
            OrderScanStatus.Completed => "check-circle-fill",
            OrderScanStatus.InProgress => "arrow-repeat",
            OrderScanStatus.NotStarted => "circle",
            _ => "circle"
        };
    }

    /// <summary>
    /// Прогрес по етапу
    /// </summary>
    public class StageProgress
    {
        public ProductionStage Stage { get; set; }
        public int TotalRequired { get; set; }
        public int Completed { get; set; }
        public int Percent { get; set; }
    }

    /// <summary>
    /// Статус сканування замовлення
    /// </summary>
    public enum OrderScanStatus
    {
        NotStarted,
        InProgress,
        Completed
    }

    /// <summary>
    /// Загальна статистика сканування
    /// </summary>
    public class OverallScanProgress
    {
        public int TotalParts { get; set; }
        public int CompletedParts { get; set; }
        public double TotalSquareMeters { get; set; }
        public double CompletedSquareMeters { get; set; }
        public int CuttingCompleted { get; set; }
        public int EdgeBandingCompleted { get; set; }
        public int DrillingCompleted { get; set; }
        public int SortingCompleted { get; set; }
        public int PackingCompleted { get; set; }
        
        public int OverallPercent => TotalParts > 0 ? (int)Math.Round((double)CompletedParts / TotalParts * 100) : 0;
    }
}
