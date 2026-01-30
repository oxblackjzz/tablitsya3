using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Background сервіс для автоматичного обслуговування БД
    /// - Архівація старих ScanLogs
    /// - Очищення завершених деталей
    /// - Оптимізація таблиць
    /// </summary>
    public class MaintenanceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MaintenanceBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(24); // Раз на добу

        public MaintenanceBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MaintenanceBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔧 MaintenanceBackgroundService started");

            // Чекаємо 5 хвилин після старту перед першим запуском
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformMaintenanceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error during maintenance task");
                }

                // Чекаємо до наступного запуску
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task PerformMaintenanceAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔧 Starting scheduled maintenance...");

            using var scope = _serviceProvider.CreateScope();
            
            try
            {
                var scanningService = scope.ServiceProvider.GetService<ScanningService>();
                
                if (scanningService != null)
                {
                    // 1. Архівація старих ScanLogs (старше 30 днів)
                    _logger.LogInformation("📦 Archiving old scan logs...");
                    var archivedLogs = await scanningService.ArchiveOldScanLogsAsync(30);
                    if (archivedLogs > 0)
                    {
                        _logger.LogInformation($"✅ Archived {archivedLogs} old scan log records");
                    }

                    // 2. Статистика ScanLogs
                    var stats = await scanningService.GetScanLogsStatisticsAsync();
                    _logger.LogInformation($"📊 ScanLogs stats: {stats.TotalRecords} total, {stats.RecordsLast24Hours} last 24h, {stats.EstimatedSize}");

                    // 3. Очищення дуже старих завершених деталей (старше 180 днів) - опціонально
                    // var cleanedParts = await scanningService.CleanupCompletedPartsAsync(180);
                }

                // 4. VACUUM для PostgreSQL (опціонально, раз на тиждень)
                if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
                {
                    await VacuumDatabaseAsync(scope);
                }

                _logger.LogInformation("✅ Scheduled maintenance completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Maintenance task failed");
            }
        }

        private async Task VacuumDatabaseAsync(IServiceScope scope)
        {
            try
            {
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                if (context != null)
                {
                    _logger.LogInformation("🧹 Running VACUUM ANALYZE on PostgreSQL...");
                    
                    // VACUUM ANALYZE оптимізує таблиці та оновлює статистику
                    await context.Database.ExecuteSqlRawAsync("VACUUM ANALYZE scan_logs");
                    await context.Database.ExecuteSqlRawAsync("VACUUM ANALYZE parts");
                    
                    _logger.LogInformation("✅ VACUUM ANALYZE completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ VACUUM failed (may not be PostgreSQL): {ex.Message}");
            }
        }
    }
}
