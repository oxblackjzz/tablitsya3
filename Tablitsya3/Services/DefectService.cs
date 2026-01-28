using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;
using Tablitsya3.Models.Scanning;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для управління дефектами (браком)
    /// </summary>
    public class DefectService
    {
        private readonly ApplicationDbContext _context;
        private readonly LoggingService _logger;

        public DefectService(ApplicationDbContext context, LoggingService logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Статуси дефектів

        public static class DefectStatus
        {
            public const string New = "new";
            public const string InProgress = "in_progress";
            public const string Repaired = "repaired";
            public const string Scrapped = "scrapped";

            public static string GetDisplayName(string status) => status switch
            {
                New => "Новий",
                InProgress => "В роботі",
                Repaired => "Виправлено",
                Scrapped => "Списано",
                _ => status
            };

            public static string GetBadgeClass(string status) => status switch
            {
                New => "bg-danger",
                InProgress => "bg-warning text-dark",
                Repaired => "bg-success",
                Scrapped => "bg-secondary",
                _ => "bg-light text-dark"
            };

            public static string GetIcon(string status) => status switch
            {
                New => "bi-exclamation-triangle",
                InProgress => "bi-wrench",
                Repaired => "bi-check-circle",
                Scrapped => "bi-trash",
                _ => "bi-question"
            };
        }

        #endregion

        #region Типи дефектів

        public static readonly List<string> DefectTypes = new()
        {
            "Скол",
            "Тріщина",
            "Подряпина",
            "Неправильний розмір",
            "Дефект матеріалу",
            "Дефект кромки",
            "Дефект свердління",
            "Забруднення",
            "Деформація",
            "Інше"
        };

        #endregion

        #region CRUD Operations

        /// <summary>
        /// Отримати всі дефекти з фільтрацією
        /// </summary>
        public async Task<(List<DefectEntity> Items, int Total)> GetDefectsAsync(
            int page = 1,
            int pageSize = 20,
            string? status = null,
            int? productionStage = null,
            string? defectType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? workerId = null)
        {
            var query = _context.Defects.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(d => d.Status == status);

            if (productionStage.HasValue)
                query = query.Where(d => d.ProductionStage == productionStage.Value);

            if (!string.IsNullOrEmpty(defectType))
                query = query.Where(d => d.DefectType == defectType);

            if (fromDate.HasValue)
                query = query.Where(d => d.CreatedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(d => d.CreatedDate <= toDate.Value.AddDays(1));

            if (workerId.HasValue)
                query = query.Where(d => d.WorkerId == workerId.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(d => d.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        /// <summary>
        /// Отримати дефект за ID
        /// </summary>
        public async Task<DefectEntity?> GetDefectByIdAsync(int id)
        {
            return await _context.Defects.FindAsync(id);
        }

        /// <summary>
        /// Отримати дефекти для деталі
        /// </summary>
        public async Task<List<DefectEntity>> GetDefectsByPartIdAsync(int partId)
        {
            return await _context.Defects
                .Where(d => d.PartId == partId)
                .OrderByDescending(d => d.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Створити новий дефект
        /// </summary>
        public async Task<DefectEntity> CreateDefectAsync(DefectEntity defect)
        {
            defect.CreatedDate = DateTime.UtcNow;
            defect.Status = DefectStatus.New;

            _context.Defects.Add(defect);
            await _context.SaveChangesAsync();

            _logger.LogInfo($"Створено дефект #{defect.Id} для деталі {defect.QRCode}, тип: {defect.DefectType}", "DefectService");

            return defect;
        }

        /// <summary>
        /// Створити дефект з QR-коду (швидке створення зі сканера)
        /// </summary>
        public async Task<DefectEntity?> CreateDefectFromQRAsync(
            string qrCode,
            string defectType,
            int productionStage,
            int? workerId = null,
            int? workstationId = null,
            string? description = null,
            int severity = 3,
            bool isRepairable = true)
        {
            // Знаходимо деталь за QR-кодом
            var part = await FindPartByQRCodeAsync(qrCode);
            
            if (part == null)
            {
                _logger.LogWarning($"Деталь не знайдена для QR-коду: {qrCode}", "DefectService");
                return null;
            }

            var defect = new DefectEntity
            {
                PartId = part.Id,
                QRCode = qrCode,
                DefectType = defectType,
                Description = description,
                ProductionStage = productionStage,
                WorkerId = workerId,
                WorkstationId = workstationId,
                Severity = severity,
                IsRepairable = isRepairable,
                Status = DefectStatus.New,
                CreatedDate = DateTime.UtcNow
            };

            _context.Defects.Add(defect);
            await _context.SaveChangesAsync();

            _logger.LogInfo($"Зареєстровано брак для деталі {part.Name} ({qrCode}), тип: {defectType}", "DefectService");

            return defect;
        }

        /// <summary>
        /// Оновити дефект
        /// </summary>
        public async Task<bool> UpdateDefectAsync(DefectEntity defect)
        {
            try
            {
                defect.UpdatedDate = DateTime.UtcNow;
                _context.Defects.Update(defect);
                await _context.SaveChangesAsync();

                _logger.LogInfo($"Оновлено дефект #{defect.Id}", "DefectService");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка оновлення дефекту #{defect.Id}: {ex.Message}", ex, "DefectService");
                return false;
            }
        }

        /// <summary>
        /// Змінити статус дефекту
        /// </summary>
        public async Task<bool> ChangeStatusAsync(int defectId, string newStatus, int? repairedByWorkerId = null, string? repairNotes = null)
        {
            var defect = await _context.Defects.FindAsync(defectId);
            if (defect == null) return false;

            var oldStatus = defect.Status;
            defect.Status = newStatus;
            defect.UpdatedDate = DateTime.UtcNow;

            if (newStatus == DefectStatus.Repaired)
            {
                defect.RepairedDate = DateTime.UtcNow;
                defect.RepairedByWorkerId = repairedByWorkerId;
                defect.RepairNotes = repairNotes;
            }

            await _context.SaveChangesAsync();

            _logger.LogInfo($"Статус дефекту #{defectId} змінено: {oldStatus} → {newStatus}", "DefectService");
            return true;
        }

        /// <summary>
        /// Позначити як виправлено
        /// </summary>
        public async Task<bool> MarkAsRepairedAsync(int defectId, int? repairedByWorkerId = null, string? repairNotes = null)
        {
            return await ChangeStatusAsync(defectId, DefectStatus.Repaired, repairedByWorkerId, repairNotes);
        }

        /// <summary>
        /// Позначити як списано
        /// </summary>
        public async Task<bool> MarkAsScrappedAsync(int defectId, string? notes = null)
        {
            var defect = await _context.Defects.FindAsync(defectId);
            if (defect == null) return false;

            defect.Status = DefectStatus.Scrapped;
            defect.RepairNotes = notes;
            defect.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInfo($"Дефект #{defectId} списано", "DefectService");
            return true;
        }

        /// <summary>
        /// Видалити дефект
        /// </summary>
        public async Task<bool> DeleteDefectAsync(int id)
        {
            var defect = await _context.Defects.FindAsync(id);
            if (defect == null) return false;

            _context.Defects.Remove(defect);
            await _context.SaveChangesAsync();

            _logger.LogInfo($"Видалено дефект #{id}", "DefectService");
            return true;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Отримати статистику дефектів
        /// </summary>
        public async Task<DefectStatistics> GetStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Defects.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(d => d.CreatedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(d => d.CreatedDate <= toDate.Value.AddDays(1));

            var defects = await query.ToListAsync();

            return new DefectStatistics
            {
                TotalCount = defects.Count,
                NewCount = defects.Count(d => d.Status == DefectStatus.New),
                InProgressCount = defects.Count(d => d.Status == DefectStatus.InProgress),
                RepairedCount = defects.Count(d => d.Status == DefectStatus.Repaired),
                ScrappedCount = defects.Count(d => d.Status == DefectStatus.Scrapped),
                ByType = defects.GroupBy(d => d.DefectType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ByStage = defects.GroupBy(d => d.ProductionStage)
                    .ToDictionary(g => ((ProductionStage)g.Key).GetDisplayName(), g => g.Count()),
                BySeverity = defects.GroupBy(d => d.Severity)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// Отримати кількість активних (нових) дефектів
        /// </summary>
        public async Task<int> GetActiveDefectsCountAsync()
        {
            return await _context.Defects
                .CountAsync(d => d.Status == DefectStatus.New || d.Status == DefectStatus.InProgress);
        }

        /// <summary>
        /// Отримати денну статистику браку
        /// </summary>
        public async Task<Dictionary<DateTime, int>> GetDailyDefectsAsync(int days = 30)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days).Date;

            var defects = await _context.Defects
                .Where(d => d.CreatedDate >= fromDate)
                .GroupBy(d => d.CreatedDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            return defects.ToDictionary(x => x.Date, x => x.Count);
        }

        #endregion

        #region Helpers

        private async Task<PartEntity?> FindPartByQRCodeAsync(string qrCode)
        {
            // Парсимо QR-код: project_uuid/part_id/part_counter
            var parts = qrCode.Split(new[] { '/', '|', '-' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 3)
            {
                var projectUuid = parts[0];
                if (int.TryParse(parts[1], out var partId) && int.TryParse(parts[2], out var partCounter))
                {
                    return await _context.Parts
                        .FirstOrDefaultAsync(p => 
                            p.ProjectExternalUuid == projectUuid && 
                            p.PartId == partId && 
                            p.PartCounter == partCounter);
                }
            }

            return null;
        }

        /// <summary>
        /// Отримати інформацію про деталь за ID дефекту
        /// </summary>
        public async Task<PartEntity?> GetPartByDefectIdAsync(int defectId)
        {
            var defect = await _context.Defects.FindAsync(defectId);
            if (defect == null) return null;

            return await _context.Parts.FindAsync(defect.PartId);
        }

        /// <summary>
        /// Отримати інформацію про працівника
        /// </summary>
        public async Task<WorkerEntity?> GetWorkerByIdAsync(int? workerId)
        {
            if (!workerId.HasValue) return null;
            return await _context.Workers.FindAsync(workerId.Value);
        }

        #endregion
    }

    #region Models

    /// <summary>
    /// Статистика дефектів
    /// </summary>
    public class DefectStatistics
    {
        public int TotalCount { get; set; }
        public int NewCount { get; set; }
        public int InProgressCount { get; set; }
        public int RepairedCount { get; set; }
        public int ScrappedCount { get; set; }
        public Dictionary<string, int> ByType { get; set; } = new();
        public Dictionary<string, int> ByStage { get; set; } = new();
        public Dictionary<int, int> BySeverity { get; set; } = new();
    }

    #endregion
}
