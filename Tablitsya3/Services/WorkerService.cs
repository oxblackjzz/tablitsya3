using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;
using Tablitsya3.Models.Scanning;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для управління працівниками
    /// </summary>
    public class WorkerService
    {
        private readonly ApplicationDbContext _context;
        private readonly LoggingService _logger;

        public WorkerService(ApplicationDbContext context, LoggingService logger)
        {
            _context = context;
            _logger = logger;
        }

        #region CRUD Operations

        /// <summary>
        /// Отримати всіх працівників
        /// </summary>
        public async Task<List<WorkerEntity>> GetAllWorkersAsync(bool includeInactive = false)
        {
            var query = _context.Workers.AsQueryable();

            if (!includeInactive)
                query = query.Where(w => w.IsActive);

            return await query
                .OrderBy(w => w.FullName)
                .ToListAsync();
        }

        /// <summary>
        /// Отримати працівників по цеху
        /// </summary>
        public async Task<List<WorkerEntity>> GetWorkersByWorkshopAsync(int workshopNumber, bool includeInactive = false)
        {
            var query = _context.Workers
                .Where(w => w.WorkshopNumber == workshopNumber);

            if (!includeInactive)
                query = query.Where(w => w.IsActive);

            return await query
                .OrderBy(w => w.FullName)
                .ToListAsync();
        }

        /// <summary>
        /// Отримати працівника за ID
        /// </summary>
        public async Task<WorkerEntity?> GetWorkerByIdAsync(int id)
        {
            return await _context.Workers.FindAsync(id);
        }

        /// <summary>
        /// Отримати працівника за кодом
        /// </summary>
        public async Task<WorkerEntity?> GetWorkerByCodeAsync(string workerCode)
        {
            return await _context.Workers
                .FirstOrDefaultAsync(w => w.WorkerCode == workerCode && w.IsActive);
        }

        /// <summary>
        /// Створити нового працівника
        /// </summary>
        public async Task<WorkerEntity> CreateWorkerAsync(WorkerEntity worker)
        {
            // Генеруємо унікальний код якщо не вказано
            if (string.IsNullOrEmpty(worker.WorkerCode))
            {
                worker.WorkerCode = await GenerateUniqueWorkerCodeAsync();
            }

            // Перевіряємо унікальність коду
            var existing = await _context.Workers
                .AnyAsync(w => w.WorkerCode == worker.WorkerCode);

            if (existing)
            {
                throw new InvalidOperationException($"Працівник з кодом '{worker.WorkerCode}' вже існує");
            }

            // Хешуємо PIN якщо вказано
            if (!string.IsNullOrEmpty(worker.PinCode))
            {
                worker.PinCodeHash = HashPin(worker.PinCode);
                worker.PinCode = null; // Не зберігаємо PIN у відкритому вигляді
            }

            // Формуємо FullName якщо не вказано
            if (string.IsNullOrEmpty(worker.FullName) && 
                (!string.IsNullOrEmpty(worker.LastName) || !string.IsNullOrEmpty(worker.FirstName)))
            {
                worker.FullName = $"{worker.LastName} {worker.FirstName} {worker.MiddleName}".Trim();
            }

            worker.CreatedDate = DateTime.UtcNow;

            _context.Workers.Add(worker);
            await _context.SaveChangesAsync();

            _logger.LogInfo($"Створено працівника: {worker.FullName} (код: {worker.WorkerCode})", "WorkerService");

            return worker;
        }

        /// <summary>
        /// Оновити працівника
        /// </summary>
        public async Task<WorkerEntity> UpdateWorkerAsync(WorkerEntity worker)
        {
            var existing = await _context.Workers.FindAsync(worker.Id);
            if (existing == null)
            {
                throw new InvalidOperationException($"Працівника з ID {worker.Id} не знайдено");
            }

            // Оновлюємо поля
            existing.WorkerCode = worker.WorkerCode;
            existing.FullName = worker.FullName;
            existing.FirstName = worker.FirstName;
            existing.LastName = worker.LastName;
            existing.MiddleName = worker.MiddleName;
            existing.Position = worker.Position;
            existing.WorkshopNumber = worker.WorkshopNumber;
            existing.AllowedStages = worker.AllowedStages;
            existing.Phone = worker.Phone;
            existing.Email = worker.Email;
            existing.HireDate = worker.HireDate;
            existing.IsActive = worker.IsActive;
            existing.Notes = worker.Notes;
            existing.UpdatedDate = DateTime.UtcNow;

            // Оновлюємо PIN тільки якщо вказано новий
            if (!string.IsNullOrEmpty(worker.PinCode))
            {
                existing.PinCodeHash = HashPin(worker.PinCode);
            }

            await _context.SaveChangesAsync();

            _logger.LogInfo($"Оновлено працівника: {existing.FullName} (ID: {existing.Id})", "WorkerService");

            return existing;
        }

        /// <summary>
        /// Деактивувати працівника
        /// </summary>
        public async Task<bool> DeactivateWorkerAsync(int id)
        {
            var worker = await _context.Workers.FindAsync(id);
            if (worker == null) return false;

            worker.IsActive = false;
            worker.UpdatedDate = DateTime.UtcNow;

            // Закриваємо активні сесії
            var activeSessions = await _context.WorkerSessions
                .Where(s => s.WorkerId == id && s.IsActive)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.IsActive = false;
                session.EndTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInfo($"Деактивовано працівника: {worker.FullName}", "WorkerService");

            return true;
        }

        /// <summary>
        /// Видалити працівника (фізичне видалення)
        /// </summary>
        public async Task<bool> DeleteWorkerAsync(int id)
        {
            var worker = await _context.Workers.FindAsync(id);
            if (worker == null) return false;

            // Перевіряємо чи немає пов'язаних сканувань
            var hasScans = await _context.ScanLogs.AnyAsync(s => s.WorkerId == id);
            if (hasScans)
            {
                throw new InvalidOperationException(
                    "Неможливо видалити працівника з існуючими скануваннями. Використайте деактивацію.");
            }

            _context.Workers.Remove(worker);
            await _context.SaveChangesAsync();

            _logger.LogInfo($"Видалено працівника: {worker.FullName}", "WorkerService");

            return true;
        }

        #endregion

        #region PIN Management

        /// <summary>
        /// Встановити PIN-код
        /// </summary>
        public async Task<bool> SetPinAsync(int workerId, string pin)
        {
            if (string.IsNullOrEmpty(pin) || pin.Length < 4 || pin.Length > 6)
            {
                throw new ArgumentException("PIN повинен містити від 4 до 6 цифр");
            }

            if (!pin.All(char.IsDigit))
            {
                throw new ArgumentException("PIN повинен містити тільки цифри");
            }

            var worker = await _context.Workers.FindAsync(workerId);
            if (worker == null) return false;

            worker.PinCodeHash = HashPin(pin);
            worker.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Перевірити PIN-код
        /// </summary>
        public async Task<bool> VerifyPinAsync(int workerId, string pin)
        {
            var worker = await _context.Workers.FindAsync(workerId);
            if (worker == null || string.IsNullOrEmpty(worker.PinCodeHash))
                return false;

            return VerifyPinHash(pin, worker.PinCodeHash);
        }

        /// <summary>
        /// Перевірити PIN за кодом працівника
        /// </summary>
        public async Task<WorkerEntity?> AuthenticateByCodeAndPinAsync(string workerCode, string pin)
        {
            var worker = await GetWorkerByCodeAsync(workerCode);
            if (worker == null) return null;

            if (string.IsNullOrEmpty(worker.PinCodeHash))
            {
                // PIN не встановлено - дозволяємо вхід без PIN
                return worker;
            }

            return VerifyPinHash(pin, worker.PinCodeHash) ? worker : null;
        }

        private string HashPin(string pin)
        {
            using var sha256 = SHA256.Create();
            var salt = "Tablitsya3_Worker_Salt_2024"; // В продакшені краще використовувати унікальну сіль для кожного працівника
            var bytes = Encoding.UTF8.GetBytes(pin + salt);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPinHash(string pin, string hash)
        {
            return HashPin(pin) == hash;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Генерувати унікальний код працівника
        /// </summary>
        public async Task<string> GenerateUniqueWorkerCodeAsync()
        {
            string code;
            var random = new Random();

            do
            {
                // Формат: W + 6 цифр (наприклад W123456)
                code = $"W{random.Next(100000, 999999)}";
            }
            while (await _context.Workers.AnyAsync(w => w.WorkerCode == code));

            return code;
        }

        /// <summary>
        /// Пошук працівників
        /// </summary>
        public async Task<List<WorkerEntity>> SearchWorkersAsync(string query, int? workshopNumber = null)
        {
            var q = _context.Workers.Where(w => w.IsActive);

            if (!string.IsNullOrEmpty(query))
            {
                query = query.ToLower();
                q = q.Where(w => 
                    w.FullName.ToLower().Contains(query) ||
                    w.WorkerCode.ToLower().Contains(query) ||
                    (w.Position != null && w.Position.ToLower().Contains(query)));
            }

            if (workshopNumber.HasValue)
            {
                q = q.Where(w => w.WorkshopNumber == workshopNumber.Value);
            }

            return await q.OrderBy(w => w.FullName).Take(20).ToListAsync();
        }

        /// <summary>
        /// Отримати кількість працівників по цехах
        /// </summary>
        public async Task<Dictionary<int, int>> GetWorkersCountByWorkshopAsync()
        {
            return await _context.Workers
                .Where(w => w.IsActive)
                .GroupBy(w => w.WorkshopNumber)
                .Select(g => new { WorkshopNumber = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.WorkshopNumber, x => x.Count);
        }

        /// <summary>
        /// Перевірити чи працівник може працювати на вказаному етапі
        /// </summary>
        public bool CanWorkerPerformStage(WorkerEntity worker, ProductionStage stage)
        {
            if (string.IsNullOrEmpty(worker.AllowedStages))
                return true; // Всі етапи дозволені за замовчуванням

            return worker.AllowedStagesList.Contains(stage);
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Отримати статистику працівника
        /// </summary>
        public async Task<WorkerStatistics?> GetWorkerStatisticsAsync(int workerId, int daysForDailyStats = 30, IEnumerable<ProductionStage>? stageFilter = null)
        {
            var worker = await GetWorkerByIdAsync(workerId);
            if (worker == null) return null;

            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
            if (weekStart > now.Date) weekStart = weekStart.AddDays(-7);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var stats = new WorkerStatistics
            {
                WorkerId = workerId,
                WorkerName = worker.DisplayName,
                WorkerCode = worker.WorkerCode,
                WorkshopNumber = worker.WorkshopNumber
            };

            // Отримуємо всі успішні сканування працівника
            var scansQuery = _context.ScanLogs
                .Where(s => s.WorkerId == workerId && s.Success);
            
            // Фільтрація по етапах
            if (stageFilter != null && stageFilter.Any())
            {
                var stageValues = stageFilter.Select(s => (int)s).ToList();
                scansQuery = scansQuery.Where(s => stageValues.Contains(s.Stage));
            }
            
            var scans = await scansQuery.ToListAsync();

            // Отримуємо інформацію про деталі для розрахунку площі
            var partIds = scans.Select(s => s.PartId).Where(id => id > 0).Distinct().ToList();
            var parts = await _context.Parts
                .Where(p => partIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => new { p.Length, p.Width });

            // Функція для розрахунку площі
            double GetSquareMeters(int partId) => 
                parts.TryGetValue(partId, out var p) ? (p.Length * p.Width) / 1_000_000 : 0;

            // Статистика за сьогодні
            var todayScans = scans.Where(s => s.ScanDate >= todayStart).ToList();
            stats.Today = CalculatePeriodStats(todayScans, GetSquareMeters);

            // Статистика за тиждень
            var weekScans = scans.Where(s => s.ScanDate >= weekStart).ToList();
            stats.Week = CalculatePeriodStats(weekScans, GetSquareMeters);

            // Статистика за місяць
            var monthScans = scans.Where(s => s.ScanDate >= monthStart).ToList();
            stats.Month = CalculatePeriodStats(monthScans, GetSquareMeters);

            // Статистика за весь час
            stats.AllTime = CalculatePeriodStats(scans, GetSquareMeters);

            // Статистика по етапах
            foreach (ProductionStage stage in Enum.GetValues<ProductionStage>())
            {
                var stageScans = scans.Where(s => s.Stage == (int)stage).ToList();
                stats.StageStats[stage] = new StageWorkerStatistics
                {
                    Stage = stage,
                    PartsProcessed = stageScans.Select(s => s.PartId).Distinct().Count(),
                    TotalSquareMeters = stageScans.Sum(s => GetSquareMeters(s.PartId)),
                    SuccessfulScans = stageScans.Count
                };
            }

            // Денна статистика для графіка
            var startDate = now.Date.AddDays(-daysForDailyStats + 1);
            stats.DailyStats = Enumerable.Range(0, daysForDailyStats)
                .Select(i => startDate.AddDays(i))
                .Select(date =>
                {
                    var dayScans = scans.Where(s => s.ScanDate.Date == date).ToList();
                    return new DailyStatistics
                    {
                        Date = date,
                        PartsProcessed = dayScans.Select(s => s.PartId).Distinct().Count(),
                        TotalSquareMeters = dayScans.Sum(s => GetSquareMeters(s.PartId)),
                        ScansCount = dayScans.Count
                    };
                })
                .ToList();

            return stats;
        }

        /// <summary>
        /// Отримати загальну статистику по всіх працівниках
        /// </summary>
        public async Task<WorkersOverallStatistics> GetOverallStatisticsAsync(IEnumerable<ProductionStage>? stageFilter = null)
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
            if (weekStart > now.Date) weekStart = weekStart.AddDays(-7);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var stats = new WorkersOverallStatistics
            {
                TotalWorkers = await _context.Workers.CountAsync(w => w.IsActive)
            };

            // Отримуємо всі успішні сканування з worker_id
            var scansQuery = _context.ScanLogs
                .Where(s => s.WorkerId != null && s.Success);
            
            // Фільтрація по етапах
            if (stageFilter != null && stageFilter.Any())
            {
                var stageValues = stageFilter.Select(s => (int)s).ToList();
                scansQuery = scansQuery.Where(s => stageValues.Contains(s.Stage));
            }
            
            var scans = await scansQuery
                .Select(s => new ScanInfo { WorkerId = s.WorkerId, PartId = s.PartId, ScanDate = s.ScanDate })
                .ToListAsync();

            // Отримуємо інформацію про деталі
            var partIds = scans.Select(s => s.PartId).Where(id => id > 0).Distinct().ToList();
            var parts = await _context.Parts
                .Where(p => partIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => new { p.Length, p.Width });

            double GetSquareMeters(int partId) => 
                parts.TryGetValue(partId, out var p) ? (p.Length * p.Width) / 1_000_000 : 0;

            // Статистика за сьогодні
            var todayScans = scans.Where(s => s.ScanDate >= todayStart).ToList();
            stats.ActiveWorkersToday = todayScans.Select(s => s.WorkerId).Distinct().Count();
            stats.TotalPartsToday = todayScans.Select(s => s.PartId).Distinct().Count();
            stats.TotalSquareMetersToday = todayScans.Sum(s => GetSquareMeters(s.PartId));

            // Статистика за тиждень
            var weekScans = scans.Where(s => s.ScanDate >= weekStart).ToList();
            stats.TotalPartsWeek = weekScans.Select(s => s.PartId).Distinct().Count();
            stats.TotalSquareMetersWeek = weekScans.Sum(s => GetSquareMeters(s.PartId));

            // Статистика за місяць
            var monthScans = scans.Where(s => s.ScanDate >= monthStart).ToList();
            stats.TotalPartsMonth = monthScans.Select(s => s.PartId).Distinct().Count();
            stats.TotalSquareMetersMonth = monthScans.Sum(s => GetSquareMeters(s.PartId));

            // Отримуємо імена працівників
            var workerIds = scans.Where(s => s.WorkerId.HasValue).Select(s => s.WorkerId!.Value).Distinct().ToList();
            var workers = await _context.Workers
                .Where(w => workerIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => (w.FullName, w.WorkshopNumber));

            // Топ працівників
            stats.TopWorkersToday = GetTopWorkers(todayScans, workers, GetSquareMeters, s => s.WorkerId, s => s.PartId, 10);
            stats.TopWorkersWeek = GetTopWorkers(weekScans, workers, GetSquareMeters, s => s.WorkerId, s => s.PartId, 10);
            stats.TopWorkersMonth = GetTopWorkers(monthScans, workers, GetSquareMeters, s => s.WorkerId, s => s.PartId, 10);

            return stats;
        }

        // Допоміжний клас для сканувань
        private class ScanInfo
        {
            public int? WorkerId { get; set; }
            public int PartId { get; set; }
            public DateTime ScanDate { get; set; }
        }

        /// <summary>
        /// Отримати статистику всіх працівників за період
        /// </summary>
        public async Task<List<WorkerStatistics>> GetAllWorkersStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var workers = await GetAllWorkersAsync(includeInactive: false);
            var result = new List<WorkerStatistics>();

            var from = fromDate ?? DateTime.UtcNow.Date.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            // Отримуємо всі сканування за період
            var scans = await _context.ScanLogs
                .Where(s => s.WorkerId != null && s.Success && s.ScanDate >= from && s.ScanDate <= to)
                .ToListAsync();

            // Отримуємо інформацію про деталі
            var partIds = scans.Select(s => s.PartId).Where(id => id > 0).Distinct().ToList();
            var parts = await _context.Parts
                .Where(p => partIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => new { p.Length, p.Width });

            double GetSquareMeters(int partId) => 
                parts.TryGetValue(partId, out var p) ? (p.Length * p.Width) / 1_000_000 : 0;

            foreach (var worker in workers)
            {
                var workerScans = scans.Where(s => s.WorkerId == worker.Id).ToList();
                
                var stats = new WorkerStatistics
                {
                    WorkerId = worker.Id,
                    WorkerName = worker.DisplayName,
                    WorkerCode = worker.WorkerCode,
                    WorkshopNumber = worker.WorkshopNumber,
                    AllTime = CalculatePeriodStats(workerScans, GetSquareMeters)
                };

                result.Add(stats);
            }

            return result.OrderByDescending(s => s.AllTime.TotalSquareMeters).ToList();
        }

        private PeriodStatistics CalculatePeriodStats(List<ScanLogEntity> scans, Func<int, double> getSquareMeters)
        {
            var stats = new PeriodStatistics
            {
                PartsProcessed = scans.Select(s => s.PartId).Distinct().Count(),
                TotalSquareMeters = scans.Sum(s => getSquareMeters(s.PartId)),
                SuccessfulScans = scans.Count(s => s.Success),
                FailedScans = scans.Count(s => !s.Success)
            };

            // Розрахунок середнього часу між скануваннями
            if (scans.Count > 1)
            {
                var orderedScans = scans.OrderBy(s => s.ScanDate).ToList();
                var totalSeconds = 0.0;
                for (int i = 1; i < orderedScans.Count; i++)
                {
                    totalSeconds += (orderedScans[i].ScanDate - orderedScans[i - 1].ScanDate).TotalSeconds;
                }
                stats.AvgTimeBetweenScans = totalSeconds / (orderedScans.Count - 1);
            }

            return stats;
        }

        private List<WorkerRanking> GetTopWorkers<T>(
            List<T> scans,
            Dictionary<int, (string FullName, int WorkshopNumber)> workers,
            Func<int, double> getSquareMeters,
            Func<T, int?> getWorkerId,
            Func<T, int> getPartId,
            int top)
        {
            return scans
                .Where(s => getWorkerId(s) != null)
                .GroupBy(s => getWorkerId(s)!.Value)
                .Select(g => new WorkerRanking
                {
                    WorkerId = g.Key,
                    WorkerName = workers.TryGetValue(g.Key, out var w) ? w.FullName : "Невідомий",
                    WorkshopNumber = workers.TryGetValue(g.Key, out var w2) ? w2.WorkshopNumber : 0,
                    PartsProcessed = g.Select(s => getPartId(s)).Distinct().Count(),
                    TotalSquareMeters = g.Sum(s => getSquareMeters(getPartId(s)))
                })
                .OrderByDescending(r => r.TotalSquareMeters)
                .Take(top)
                .Select((r, i) => { r.Rank = i + 1; return r; })
                .ToList();
        }

        #endregion
    }
}
