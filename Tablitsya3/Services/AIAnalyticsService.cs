using Microsoft.ML;
using Microsoft.ML.Data;
using Tablitsya3.Data;
using Microsoft.EntityFrameworkCore;

namespace Tablitsya3.Services;

/// <summary>
/// AI Аналітика на базі ML.NET
/// </summary>
public class AIAnalyticsService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIAnalyticsService> _logger;
    private readonly MLContext _mlContext;

    public AIAnalyticsService(
        IServiceProvider serviceProvider,
        ILogger<AIAnalyticsService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _mlContext = new MLContext(seed: 42);
    }

    /// <summary>
    /// Прогноз продуктивності працівника на наступний день
    /// </summary>
    public async Task<WorkerProductivityPrediction> PredictWorkerProductivityAsync(int workerId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Отримуємо історію сканувань працівника за останні 30 днів
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var scanHistory = await dbContext.ScanLogs
                .Where(s => s.WorkerId == workerId && s.ScanDate >= thirtyDaysAgo)
                .GroupBy(s => s.ScanDate.Date)
                .Select(g => new DailyProductivityData
                {
                    DayOfWeek = (float)g.Key.DayOfWeek,
                    DayOfMonth = g.Key.Day,
                    PartsScanned = g.Count()
                })
                .ToListAsync();

            if (scanHistory.Count < 7)
            {
                return new WorkerProductivityPrediction
                {
                    WorkerId = workerId,
                    PredictedParts = 0,
                    Confidence = 0,
                    Message = "Недостатньо даних (потрібно мінімум 7 днів)",
                    HasEnoughData = false
                };
            }

            // Навчаємо модель
            var dataView = _mlContext.Data.LoadFromEnumerable(scanHistory);

            var pipeline = _mlContext.Transforms.Concatenate("Features", 
                    nameof(DailyProductivityData.DayOfWeek),
                    nameof(DailyProductivityData.DayOfMonth))
                .Append(_mlContext.Regression.Trainers.Sdca(
                    labelColumnName: nameof(DailyProductivityData.PartsScanned),
                    featureColumnName: "Features"));

            var model = pipeline.Fit(dataView);

            // Прогноз на завтра
            var tomorrow = DateTime.Now.AddDays(1);
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<DailyProductivityData, ProductivityPredictionResult>(model);
            
            var prediction = predictionEngine.Predict(new DailyProductivityData
            {
                DayOfWeek = (float)tomorrow.DayOfWeek,
                DayOfMonth = tomorrow.Day
            });

            // Розраховуємо впевненість
            var avgParts = scanHistory.Average(s => s.PartsScanned);
            var confidence = Math.Min(1.0, scanHistory.Count / 30.0);

            return new WorkerProductivityPrediction
            {
                WorkerId = workerId,
                PredictedParts = Math.Max(0, (int)prediction.PredictedParts),
                AverageParts = (int)avgParts,
                Confidence = confidence,
                Message = $"Прогноз на {tomorrow:dd.MM.yyyy} ({GetDayName(tomorrow.DayOfWeek)})",
                HasEnoughData = true,
                DataDays = scanHistory.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting worker productivity for worker {WorkerId}", workerId);
            return new WorkerProductivityPrediction
            {
                WorkerId = workerId,
                Message = $"Помилка: {ex.Message}",
                HasEnoughData = false
            };
        }
    }

    /// <summary>
    /// Виявлення аномалій в сканування (незвичайно мало/багато)
    /// </summary>
    public async Task<List<AnomalyAlert>> DetectAnomaliesAsync()
    {
        var alerts = new List<AnomalyAlert>();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Отримуємо статистику за останні 14 днів
            var fourteenDaysAgo = DateTime.UtcNow.AddDays(-14);
            var dailyStats = await dbContext.ScanLogs
                .Where(s => s.ScanDate >= fourteenDaysAgo)
                .GroupBy(s => s.ScanDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            if (dailyStats.Count < 5)
                return alerts;

            // Розраховуємо середнє та стандартне відхилення
            var counts = dailyStats.Select(x => (double)x.Count).ToList();
            var mean = counts.Average();
            var stdDev = Math.Sqrt(counts.Average(x => Math.Pow(x - mean, 2)));

            // Перевіряємо вчорашній день
            var yesterday = DateTime.Today.AddDays(-1);
            var yesterdayStat = dailyStats.FirstOrDefault(x => x.Date == yesterday);

            if (yesterdayStat != null && stdDev > 0)
            {
                var zScore = (yesterdayStat.Count - mean) / stdDev;

                if (zScore < -2) // Значно менше норми
                {
                    alerts.Add(new AnomalyAlert
                    {
                        Type = AnomalyType.LowProductivity,
                        Severity = zScore < -3 ? "critical" : "warning",
                        Message = $"Вчора ({yesterday:dd.MM}) відскановано лише {yesterdayStat.Count} деталей (норма: {mean:F0}±{stdDev:F0})",
                        Value = yesterdayStat.Count,
                        ExpectedValue = mean,
                        Deviation = zScore
                    });
                }
                else if (zScore > 2) // Значно більше норми
                {
                    alerts.Add(new AnomalyAlert
                    {
                        Type = AnomalyType.HighProductivity,
                        Severity = "info",
                        Message = $"Вчора ({yesterday:dd.MM}) рекордна продуктивність: {yesterdayStat.Count} деталей! (норма: {mean:F0})",
                        Value = yesterdayStat.Count,
                        ExpectedValue = mean,
                        Deviation = zScore
                    });
                }
            }

            // Перевіряємо брак
            var defectStats = await dbContext.Defects
                .Where(d => d.CreatedDate >= fourteenDaysAgo)
                .GroupBy(d => d.CreatedDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            if (defectStats.Any())
            {
                var defectCounts = defectStats.Select(x => (double)x.Count).ToList();
                var defectMean = defectCounts.Average();
                var defectStdDev = defectCounts.Count > 1 
                    ? Math.Sqrt(defectCounts.Average(x => Math.Pow(x - defectMean, 2)))
                    : 0;

                var yesterdayDefects = defectStats.FirstOrDefault(x => x.Date == yesterday);
                if (yesterdayDefects != null && defectStdDev > 0)
                {
                    var zScore = (yesterdayDefects.Count - defectMean) / defectStdDev;
                    if (zScore > 2)
                    {
                        alerts.Add(new AnomalyAlert
                        {
                            Type = AnomalyType.HighDefectRate,
                            Severity = zScore > 3 ? "critical" : "warning",
                            Message = $"Вчора ({yesterday:dd.MM}) зафіксовано {yesterdayDefects.Count} дефектів (норма: {defectMean:F1})",
                            Value = yesterdayDefects.Count,
                            ExpectedValue = defectMean,
                            Deviation = zScore
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting anomalies");
        }

        return alerts;
    }

    /// <summary>
    /// Загальна AI статистика системи
    /// </summary>
    public async Task<AISystemStats> GetSystemStatsAsync()
    {
        var stats = new AISystemStats();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // Загальна кількість даних
            stats.TotalScans = await dbContext.ScanLogs.CountAsync();
            stats.TotalDefects = await dbContext.Defects.CountAsync();
            stats.TotalWorkers = await dbContext.Workers.CountAsync();
            stats.TotalParts = await dbContext.Parts.CountAsync();

            // Дані за 30 днів
            stats.ScansLast30Days = await dbContext.ScanLogs
                .Where(s => s.ScanDate >= thirtyDaysAgo)
                .CountAsync();

            stats.DefectsLast30Days = await dbContext.Defects
                .Where(d => d.CreatedDate >= thirtyDaysAgo)
                .CountAsync();

            // Чи достатньо даних для ML
            stats.HasEnoughDataForPredictions = stats.ScansLast30Days >= 100;
            stats.HasEnoughDataForAnomalies = stats.ScansLast30Days >= 50;

            // Рекомендації
            if (stats.ScansLast30Days < 50)
            {
                stats.Recommendations.Add("📊 Потрібно більше даних: мінімум 50 сканувань для базової аналітики");
            }
            if (stats.ScansLast30Days < 100)
            {
                stats.Recommendations.Add("🔮 Для точних прогнозів потрібно 100+ сканувань за 30 днів");
            }
            if (stats.TotalWorkers < 3)
            {
                stats.Recommendations.Add("👷 Додайте більше працівників для порівняльної аналітики");
            }

            // Середня продуктивність
            if (stats.ScansLast30Days > 0)
            {
                var dailyAvg = await dbContext.ScanLogs
                    .Where(s => s.ScanDate >= thirtyDaysAgo)
                    .GroupBy(s => s.ScanDate.Date)
                    .Select(g => g.Count())
                    .AverageAsync();
                stats.AverageDailyScans = (int)dailyAvg;
            }

            // Тренд (порівняння з попереднім періодом)
            var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);
            var previousPeriodScans = await dbContext.ScanLogs
                .Where(s => s.ScanDate >= sixtyDaysAgo && s.ScanDate < thirtyDaysAgo)
                .CountAsync();

            if (previousPeriodScans > 0)
            {
                stats.ProductivityTrend = ((double)stats.ScansLast30Days - previousPeriodScans) / previousPeriodScans * 100;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI system stats");
        }

        return stats;
    }

    /// <summary>
    /// Топ працівників з прогнозами
    /// </summary>
    public async Task<List<WorkerAIInsight>> GetTopWorkersWithInsightsAsync(int count = 5)
    {
        var insights = new List<WorkerAIInsight>();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var topWorkers = await dbContext.ScanLogs
                .Where(s => s.ScanDate >= thirtyDaysAgo && s.WorkerId != null)
                .GroupBy(s => s.WorkerId)
                .Select(g => new { WorkerId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .ToListAsync();

            foreach (var worker in topWorkers)
            {
                if (worker.WorkerId == null) continue;

                var workerEntity = await dbContext.Workers.FindAsync(worker.WorkerId.Value);
                if (workerEntity == null) continue;

                var prediction = await PredictWorkerProductivityAsync(worker.WorkerId.Value);

                insights.Add(new WorkerAIInsight
                {
                    WorkerId = worker.WorkerId.Value,
                    WorkerName = workerEntity.FullName,
                    TotalScansLast30Days = worker.Count,
                    PredictedTomorrow = prediction.PredictedParts,
                    AverageDaily = prediction.AverageParts,
                    Trend = prediction.PredictedParts > prediction.AverageParts ? "up" : 
                            prediction.PredictedParts < prediction.AverageParts ? "down" : "stable"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top workers with insights");
        }

        return insights;
    }

    private static string GetDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Понеділок",
        DayOfWeek.Tuesday => "Вівторок",
        DayOfWeek.Wednesday => "Середа",
        DayOfWeek.Thursday => "Четвер",
        DayOfWeek.Friday => "П'ятниця",
        DayOfWeek.Saturday => "Субота",
        DayOfWeek.Sunday => "Неділя",
        _ => day.ToString()
    };
}

#region ML.NET Data Classes

public class DailyProductivityData
{
    public float DayOfWeek { get; set; }
    public float DayOfMonth { get; set; }
    
    [ColumnName("Label")]
    public float PartsScanned { get; set; }
}

public class ProductivityPredictionResult
{
    [ColumnName("Score")]
    public float PredictedParts { get; set; }
}

#endregion

#region Result Classes

public class WorkerProductivityPrediction
{
    public int WorkerId { get; set; }
    public int PredictedParts { get; set; }
    public int AverageParts { get; set; }
    public double Confidence { get; set; }
    public string Message { get; set; } = "";
    public bool HasEnoughData { get; set; }
    public int DataDays { get; set; }
}

public class AnomalyAlert
{
    public AnomalyType Type { get; set; }
    public string Severity { get; set; } = "info"; // info, warning, critical
    public string Message { get; set; } = "";
    public double Value { get; set; }
    public double ExpectedValue { get; set; }
    public double Deviation { get; set; }
}

public enum AnomalyType
{
    LowProductivity,
    HighProductivity,
    HighDefectRate,
    LowDefectRate,
    UnusualPattern
}

public class AISystemStats
{
    public int TotalScans { get; set; }
    public int TotalDefects { get; set; }
    public int TotalWorkers { get; set; }
    public int TotalParts { get; set; }
    public int ScansLast30Days { get; set; }
    public int DefectsLast30Days { get; set; }
    public int AverageDailyScans { get; set; }
    public double ProductivityTrend { get; set; } // % зміна
    public bool HasEnoughDataForPredictions { get; set; }
    public bool HasEnoughDataForAnomalies { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

public class WorkerAIInsight
{
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = "";
    public int TotalScansLast30Days { get; set; }
    public int PredictedTomorrow { get; set; }
    public int AverageDaily { get; set; }
    public string Trend { get; set; } = "stable"; // up, down, stable
}

#endregion
