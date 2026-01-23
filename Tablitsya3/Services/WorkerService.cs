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
    }
}
