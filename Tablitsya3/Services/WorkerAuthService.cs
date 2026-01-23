using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;
using Tablitsya3.Models.Scanning;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Результат авторизації працівника
    /// </summary>
    public class WorkerAuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public WorkerEntity? Worker { get; set; }
        public WorkstationEntity? Workstation { get; set; }
        public WorkerSessionEntity? Session { get; set; }
        public string? SessionToken { get; set; }
    }

    /// <summary>
    /// Контекст авторизованого працівника для сканування
    /// </summary>
    public class WorkerScanContext
    {
        public int WorkerId { get; set; }
        public int WorkstationId { get; set; }
        public int SessionId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public string WorkstationName { get; set; } = string.Empty;
        public ProductionStage Stage { get; set; }
        public int WorkshopNumber { get; set; }
    }

    /// <summary>
    /// Сервіс авторизації працівників на робочих станціях
    /// </summary>
    public class WorkerAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly WorkerService _workerService;
        private readonly WorkstationService _workstationService;
        private readonly LoggingService _logger;

        public WorkerAuthService(
            ApplicationDbContext context,
            WorkerService workerService,
            WorkstationService workstationService,
            LoggingService logger)
        {
            _context = context;
            _workerService = workerService;
            _workstationService = workstationService;
            _logger = logger;
        }

        #region Authentication

        /// <summary>
        /// Авторизація працівника на станції за кодом та PIN
        /// </summary>
        public async Task<WorkerAuthResult> LoginAsync(
            string workerCode,
            string? pin,
            int workstationId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var result = new WorkerAuthResult();

            try
            {
                // Перевіряємо станцію
                var workstation = await _workstationService.GetWorkstationByIdAsync(workstationId);
                if (workstation == null || !workstation.IsActive)
                {
                    result.Message = "Станцію не знайдено або вона неактивна";
                    return result;
                }

                result.Workstation = workstation;

                // Шукаємо працівника
                var worker = await _workerService.GetWorkerByCodeAsync(workerCode);
                if (worker == null)
                {
                    result.Message = "Працівника з таким кодом не знайдено";
                    _logger.LogWarning($"Спроба входу з невідомим кодом: {workerCode}", "WorkerAuthService");
                    return result;
                }

                result.Worker = worker;

                // Перевіряємо PIN якщо потрібно
                if (workstation.RequiresWorkerAuth && !string.IsNullOrEmpty(worker.PinCodeHash))
                {
                    if (string.IsNullOrEmpty(pin))
                    {
                        result.Message = "Введіть PIN-код";
                        return result;
                    }

                    var pinValid = await _workerService.VerifyPinAsync(worker.Id, pin);
                    if (!pinValid)
                    {
                        result.Message = "Невірний PIN-код";
                        _logger.LogWarning($"Невірний PIN для працівника: {worker.FullName}", "WorkerAuthService");
                        return result;
                    }
                }

                // Перевіряємо чи працівник може працювати на цьому етапі
                var stage = (ProductionStage)workstation.ProductionStage;
                if (!_workerService.CanWorkerPerformStage(worker, stage))
                {
                    result.Message = $"Працівник не має доступу до етапу '{stage.GetDisplayName()}'";
                    return result;
                }

                // Закриваємо попередні сесії цього працівника
                await CloseWorkerSessionsAsync(worker.Id);

                // Закриваємо попередню сесію на станції (якщо інший працівник)
                await CloseWorkstationSessionAsync(workstationId);

                // Створюємо нову сесію
                var session = new WorkerSessionEntity
                {
                    WorkerId = worker.Id,
                    WorkstationId = workstationId,
                    SessionToken = GenerateSessionToken(),
                    StartTime = DateTime.UtcNow,
                    IsActive = true,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    ScansCount = 0
                };

                _context.WorkerSessions.Add(session);
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Session = session;
                result.SessionToken = session.SessionToken;
                result.Message = $"Вітаємо, {worker.DisplayName}! Ви авторизовані на станції '{workstation.Name}'";

                _logger.LogInfo($"Працівник {worker.FullName} авторизувався на станції {workstation.Name}", "WorkerAuthService");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка авторизації: {ex.Message}", ex, "WorkerAuthService");
                result.Message = $"Помилка авторизації: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Авторизація за QR-кодом бейджа працівника
        /// </summary>
        public async Task<WorkerAuthResult> LoginByBadgeAsync(
            string badgeQRCode,
            int workstationId,
            string? ipAddress = null,
            string? userAgent = null)
        {
            // QR-код бейджа може містити код працівника або спеціальний формат
            // Формат: WORKER:{code}:{pin} або просто код працівника
            
            string workerCode;
            string? pin = null;

            if (badgeQRCode.StartsWith("WORKER:"))
            {
                var parts = badgeQRCode.Split(':');
                workerCode = parts.Length > 1 ? parts[1] : string.Empty;
                pin = parts.Length > 2 ? parts[2] : null;
            }
            else
            {
                workerCode = badgeQRCode;
            }

            return await LoginAsync(workerCode, pin, workstationId, ipAddress, userAgent);
        }

        /// <summary>
        /// Вихід працівника зі станції
        /// </summary>
        public async Task<bool> LogoutAsync(string sessionToken)
        {
            var session = await _context.WorkerSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

            if (session == null) return false;

            session.IsActive = false;
            session.EndTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInfo($"Завершено сесію {session.Id} для працівника {session.WorkerId}", "WorkerAuthService");

            return true;
        }

        /// <summary>
        /// Вихід працівника за ID
        /// </summary>
        public async Task<bool> LogoutByWorkerIdAsync(int workerId)
        {
            return await CloseWorkerSessionsAsync(workerId);
        }

        /// <summary>
        /// Закрити всі сесії на станції
        /// </summary>
        public async Task<bool> LogoutFromWorkstationAsync(int workstationId)
        {
            return await CloseWorkstationSessionAsync(workstationId);
        }

        #endregion

        #region Session Validation

        /// <summary>
        /// Перевірити валідність сесії за токеном
        /// </summary>
        public async Task<WorkerScanContext?> ValidateSessionAsync(string sessionToken)
        {
            var session = await _context.WorkerSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

            if (session == null) return null;

            var workstation = await _context.Workstations.FindAsync(session.WorkstationId);
            if (workstation == null || !workstation.IsActive) return null;

            // Перевіряємо тайм-аут
            if (session.IsExpired(workstation.SessionTimeoutMinutes))
            {
                session.IsActive = false;
                session.EndTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return null;
            }

            var worker = await _context.Workers.FindAsync(session.WorkerId);
            if (worker == null || !worker.IsActive) return null;

            return new WorkerScanContext
            {
                WorkerId = worker.Id,
                WorkstationId = workstation.Id,
                SessionId = session.Id,
                WorkerName = worker.DisplayName,
                WorkstationName = workstation.Name,
                Stage = (ProductionStage)workstation.ProductionStage,
                WorkshopNumber = workstation.WorkshopNumber
            };
        }

        /// <summary>
        /// Отримати контекст сканування для станції
        /// </summary>
        public async Task<WorkerScanContext?> GetScanContextForWorkstationAsync(int workstationId)
        {
            var session = await _workstationService.GetActiveSessionAsync(workstationId);
            if (session == null) return null;

            return await ValidateSessionAsync(session.SessionToken);
        }

        /// <summary>
        /// Оновити активність сесії (подовжити тайм-аут)
        /// </summary>
        public async Task UpdateSessionActivityAsync(int sessionId)
        {
            var session = await _context.WorkerSessions.FindAsync(sessionId);
            if (session != null && session.IsActive)
            {
                session.LastScanTime = DateTime.UtcNow;
                session.ScansCount++;
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Отримати всі активні сесії на всіх станціях
        /// </summary>
        public async Task<List< (WorkstationEntity Workstation, WorkerSessionEntity Session, WorkerEntity Worker)>> GetAllActiveSessionsAsync()
        {
            return await _workstationService.GetAllActiveSessionsAsync();
        }

        /// <summary>
        /// Отримати історію сесій працівника
        /// </summary>
        public async Task<List<WorkerSessionEntity>> GetWorkerSessionHistoryAsync(int workerId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.WorkerSessions
                .Where(s => s.WorkerId == workerId);

            if (fromDate.HasValue)
                query = query.Where(s => s.StartTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.StartTime <= toDate.Value);

            return await query
                .OrderByDescending(s => s.StartTime)
                .Take(100)
                .ToListAsync();
        }

        /// <summary>
        /// Отримати статистику робочого часу працівника
        /// </summary>
        public async Task<WorkerTimeStatistics> GetWorkerTimeStatisticsAsync(int workerId, DateTime fromDate, DateTime toDate)
        {
            var sessions = await _context.WorkerSessions
                .Where(s => s.WorkerId == workerId && s.StartTime >= fromDate && s.StartTime <= toDate)
                .ToListAsync();

            var stats = new WorkerTimeStatistics
            {
                WorkerId = workerId,
                FromDate = fromDate,
                ToDate = toDate,
                TotalSessions = sessions.Count,
                TotalScans = sessions.Sum(s => s.ScansCount)
            };

            foreach (var session in sessions)
            {
                var duration = session.Duration;
                stats.TotalWorkMinutes += (int)duration.TotalMinutes;
            }

            // Групуємо по днях
            var dailyGroups = sessions.GroupBy(s => s.StartTime.Date);
            stats.WorkDays = dailyGroups.Count();

            return stats;
        }

        #endregion

        #region Private Helpers

        private async Task<bool> CloseWorkerSessionsAsync(int workerId)
        {
            var activeSessions = await _context.WorkerSessions
                .Where(s => s.WorkerId == workerId && s.IsActive)
                .ToListAsync();

            if (!activeSessions.Any()) return false;

            foreach (var session in activeSessions)
            {
                session.IsActive = false;
                session.EndTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> CloseWorkstationSessionAsync(int workstationId)
        {
            var activeSessions = await _context.WorkerSessions
                .Where(s => s.WorkstationId == workstationId && s.IsActive)
                .ToListAsync();

            if (!activeSessions.Any()) return false;

            foreach (var session in activeSessions)
            {
                session.IsActive = false;
                session.EndTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateSessionToken()
        {
            return $"{Guid.NewGuid():N}{DateTime.UtcNow.Ticks}";
        }

        #endregion
    }

    /// <summary>
    /// Статистика робочого часу працівника
    /// </summary>
    public class WorkerTimeStatistics
    {
        public int WorkerId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalSessions { get; set; }
        public int TotalWorkMinutes { get; set; }
        public int TotalScans { get; set; }
        public int WorkDays { get; set; }

        public TimeSpan TotalWorkTime => TimeSpan.FromMinutes(TotalWorkMinutes);
        public double AvgMinutesPerDay => WorkDays > 0 ? (double)TotalWorkMinutes / WorkDays : 0;
        public double AvgScansPerSession => TotalSessions > 0 ? (double)TotalScans / TotalSessions : 0;
    }
}
