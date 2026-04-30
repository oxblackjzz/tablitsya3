using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;
using Tablitsya3.Models.Scanning;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для управління робочими станціями/станками
    /// </summary>
    public class WorkstationService
    {
        private readonly ApplicationDbContext _context;
        private readonly LoggingService _logger;

        public WorkstationService(ApplicationDbContext context, LoggingService logger)
        {
            _context = context;
            _logger = logger;
        }

        #region CRUD Operations

        /// <summary>
        /// Отримати всі станції
        /// </summary>
        public async Task<List<WorkstationEntity>> GetAllWorkstationsAsync(bool includeInactive = false)
        {
            var query = _context.Workstations.AsQueryable();

            if (!includeInactive)
                query = query.Where(w => w.IsActive);

            return await query
                .OrderBy(w => w.WorkshopNumber)
                .ThenBy(w => w.ProductionStage)
                .ThenBy(w => w.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Отримати станції по цеху
        /// </summary>
        public async Task<List<WorkstationEntity>> GetWorkstationsByWorkshopAsync(int workshopNumber, bool includeInactive = false)
        {
            var query = _context.Workstations
                .Where(w => w.WorkshopNumber == workshopNumber);

            if (!includeInactive)
                query = query.Where(w => w.IsActive);

            return await query
                .OrderBy(w => w.ProductionStage)
                .ThenBy(w => w.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Отримати станції по етапу виробництва
        /// </summary>
        public async Task<List<WorkstationEntity>> GetWorkstationsByStageAsync(ProductionStage stage, int? workshopNumber = null)
        {
            var query = _context.Workstations
                .Where(w => w.ProductionStage == (int)stage && w.IsActive);

            if (workshopNumber.HasValue)
                query = query.Where(w => w.WorkshopNumber == workshopNumber.Value);

            return await query
                .OrderBy(w => w.WorkshopNumber)
                .ThenBy(w => w.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Отримати станцію за ID
        /// </summary>
        public async Task<WorkstationEntity?> GetWorkstationByIdAsync(int id)
        {
            return await _context.Workstations.FindAsync(id);
        }

        /// <summary>
        /// Отримати станцію за кодом
        /// </summary>
        public async Task<WorkstationEntity?> GetWorkstationByCodeAsync(string stationCode)
        {
            return await _context.Workstations
                .FirstOrDefaultAsync(w => w.StationCode == stationCode && w.IsActive);
        }

        /// <summary>
        /// Отримати станцію за ідентифікатором пристрою
        /// </summary>
        public async Task<WorkstationEntity?> GetWorkstationByDeviceIdentifierAsync(string deviceIdentifier)
        {
            return await _context.Workstations
                .FirstOrDefaultAsync(w => w.DeviceIdentifier == deviceIdentifier && w.IsActive);
        }

        /// <summary>
        /// Створити нову станцію
        /// </summary>
        public async Task<WorkstationEntity> CreateWorkstationAsync(WorkstationEntity workstation)
        {
            // Генеруємо унікальний код якщо не вказано
            if (string.IsNullOrEmpty(workstation.StationCode))
            {
                workstation.StationCode = await GenerateUniqueStationCodeAsync(workstation.WorkshopNumber, (ProductionStage)workstation.ProductionStage);
            }

            // Перевіряємо унікальність коду
            var existing = await _context.Workstations
                .AnyAsync(w => w.StationCode == workstation.StationCode);

            if (existing)
            {
                throw new InvalidOperationException($"Станція з кодом '{workstation.StationCode}' вже існує");
            }

            workstation.CreatedDate = DateTime.UtcNow;

            _context.Workstations.Add(workstation);
            await _context.SaveChangesAsync();

            _logger.LogInfo($"Створено станцію: {workstation.Name} (код: {workstation.StationCode})", "WorkstationService");

            return workstation;
        }

        /// <summary>
        /// Оновити станцію
        /// </summary>
        public async Task<WorkstationEntity> UpdateWorkstationAsync(WorkstationEntity workstation)
        {
            var existing = await _context.Workstations.FindAsync(workstation.Id);
            if (existing == null)
            {
                throw new InvalidOperationException($"Станцію з ID {workstation.Id} не знайдено");
            }

            // Оновлюємо поля
            existing.StationCode = workstation.StationCode;
            existing.Name = workstation.Name;
            existing.Description = workstation.Description;
            existing.WorkshopNumber = workstation.WorkshopNumber;
            existing.ProductionStage = workstation.ProductionStage;
            existing.Location = workstation.Location;
            existing.IsActive = workstation.IsActive;
            existing.RequiresWorkerAuth = workstation.RequiresWorkerAuth;
            existing.SessionTimeoutMinutes = workstation.SessionTimeoutMinutes;
            existing.DeviceIdentifier = workstation.DeviceIdentifier;
            existing.Capacity = workstation.Capacity;
            existing.ScannerModel = workstation.ScannerModel;
            existing.ScannerConnectionType = workstation.ScannerConnectionType;
            existing.ScannerEnabled = workstation.ScannerEnabled;
            existing.ScannerDeviceName = workstation.ScannerDeviceName;
            existing.ScannerSerialNumber = workstation.ScannerSerialNumber;
            existing.ScannerUsbVid = workstation.ScannerUsbVid;
            existing.ScannerUsbPid = workstation.ScannerUsbPid;
            existing.ScannerComPort = workstation.ScannerComPort;
            existing.ScannerBaudRate = workstation.ScannerBaudRate;
            existing.ScannerBluetoothMac = workstation.ScannerBluetoothMac;
            existing.ScannerIpAddress = workstation.ScannerIpAddress;
            existing.ScannerTcpPort = workstation.ScannerTcpPort;
            existing.ScannerWebhookUrl = workstation.ScannerWebhookUrl;
            existing.ScannerPrefix = workstation.ScannerPrefix;
            existing.ScannerSuffix = workstation.ScannerSuffix;
            existing.ScannerExtraJson = workstation.ScannerExtraJson;
            existing.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInfo($"Оновлено станцію: {existing.Name} (ID: {existing.Id})", "WorkstationService");

            return existing;
        }

        /// <summary>
        /// Деактивувати станцію
        /// </summary>
        public async Task<bool> DeactivateWorkstationAsync(int id)
        {
            var workstation = await _context.Workstations.FindAsync(id);
            if (workstation == null) return false;

            workstation.IsActive = false;
            workstation.UpdatedDate = DateTime.UtcNow;

            // Закриваємо активні сесії на цій станції
            var activeSessions = await _context.WorkerSessions
                .Where(s => s.WorkstationId == id && s.IsActive)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.IsActive = false;
                session.EndTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInfo($"Деактивовано станцію: {workstation.Name}", "WorkstationService");

            return true;
        }

        /// <summary>
        /// Видалити станцію
        /// </summary>
        public async Task<bool> DeleteWorkstationAsync(int id)
        {
            var workstation = await _context.Workstations.FindAsync(id);
            if (workstation == null) return false;

            // Перевіряємо чи немає пов'язаних сканувань
            var hasScans = await _context.ScanLogs.AnyAsync(s => s.WorkstationId == id);
            if (hasScans)
            {
                throw new InvalidOperationException(
                    "Неможливо видалити станцію з існуючими скануваннями. Використайте деактивацію.");
            }

            _context.Workstations.Remove(workstation);
            await _context.SaveChangesAsync();

            _logger.LogInfo($"Видалено станцію: {workstation.Name}", "WorkstationService");

            return true;
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Отримати активну сесію на станції
        /// </summary>
        public async Task<WorkerSessionEntity?> GetActiveSessionAsync(int workstationId)
        {
            var workstation = await GetWorkstationByIdAsync(workstationId);
            if (workstation == null) return null;

            var session = await _context.WorkerSessions
                .Where(s => s.WorkstationId == workstationId && s.IsActive)
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefaultAsync();

            if (session == null) return null;

            // Перевіряємо чи не закінчився тайм-аут
            if (session.IsExpired(workstation.SessionTimeoutMinutes))
            {
                session.IsActive = false;
                session.EndTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return null;
            }

            return session;
        }

        /// <summary>
        /// Отримати список активних сесій на всіх станціях
        /// </summary>
        public async Task<List<(WorkstationEntity Workstation, WorkerSessionEntity Session, WorkerEntity Worker)>> GetAllActiveSessionsAsync()
        {
            var result = new List<(WorkstationEntity, WorkerSessionEntity, WorkerEntity)>();

            var activeSessions = await _context.WorkerSessions
                .Where(s => s.IsActive)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                var workstation = await _context.Workstations.FindAsync(session.WorkstationId);
                var worker = await _context.Workers.FindAsync(session.WorkerId);

                if (workstation != null && worker != null)
                {
                    // Перевіряємо тайм-аут
                    if (session.IsExpired(workstation.SessionTimeoutMinutes))
                    {
                        session.IsActive = false;
                        session.EndTime = DateTime.UtcNow;
                    }
                    else
                    {
                        result.Add((workstation, session, worker));
                    }
                }
            }

            await _context.SaveChangesAsync();

            return result;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Генерувати унікальний код станції
        /// </summary>
        public async Task<string> GenerateUniqueStationCodeAsync(int workshopNumber, ProductionStage stage)
        {
            // Формат: WS{цех}-{етап}{номер} (наприклад WS1-CUT01)
            var stagePrefix = stage switch
            {
                ProductionStage.Cutting => "CUT",
                ProductionStage.EdgeBanding => "EDG",
                ProductionStage.Drilling => "DRL",
                ProductionStage.Sorting => "SRT",
                ProductionStage.Packing => "PCK",
                _ => "STN"
            };

            var prefix = $"WS{workshopNumber}-{stagePrefix}";

            // Знаходимо максимальний номер
            var existingCodes = await _context.Workstations
                .Where(w => w.StationCode.StartsWith(prefix))
                .Select(w => w.StationCode)
                .ToListAsync();

            var maxNumber = 0;
            foreach (var code in existingCodes)
            {
                var numberPart = code.Substring(prefix.Length);
                if (int.TryParse(numberPart, out var num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }

            return $"{prefix}{(maxNumber + 1):D2}";
        }

        /// <summary>
        /// Отримати кількість станцій по етапах
        /// </summary>
        public async Task<Dictionary<ProductionStage, int>> GetWorkstationsCountByStageAsync(int? workshopNumber = null)
        {
            var query = _context.Workstations.Where(w => w.IsActive);

            if (workshopNumber.HasValue)
                query = query.Where(w => w.WorkshopNumber == workshopNumber.Value);

            var groups = await query
                .GroupBy(w => w.ProductionStage)
                .Select(g => new { Stage = g.Key, Count = g.Count() })
                .ToListAsync();

            return groups.ToDictionary(
                x => (ProductionStage)x.Stage,
                x => x.Count);
        }

        /// <summary>
        /// Створити станції за замовчуванням для цеху
        /// </summary>
        public async Task<List<WorkstationEntity>> CreateDefaultWorkstationsAsync(int workshopNumber)
        {
            var createdStations = new List<WorkstationEntity>();

            foreach (ProductionStage stage in Enum.GetValues<ProductionStage>())
            {
                var station = new WorkstationEntity
                {
                    Name = $"{stage.GetDisplayName()} - Цех {workshopNumber}",
                    WorkshopNumber = workshopNumber,
                    ProductionStage = (int)stage,
                    RequiresWorkerAuth = true,
                    SessionTimeoutMinutes = 60,
                    IsActive = true
                };

                var created = await CreateWorkstationAsync(station);
                createdStations.Add(created);
            }

            _logger.LogInfo($"Створено {createdStations.Count} станцій за замовчуванням для цеху {workshopNumber}", "WorkstationService");

            return createdStations;
        }

        /// <summary>
        /// Розрахувати автоматичну потужність цеху від станцій.
        /// Потужність = мінімум з сум потужностей по кожному етапу виробництва.
        /// </summary>
        public async Task<int> CalculateAutoCapacityAsync(int workshopNumber)
        {
            var stations = await _context.Workstations
                .Where(w => w.WorkshopNumber == workshopNumber && w.IsActive && w.Capacity > 0)
                .ToListAsync();

            if (!stations.Any())
                return 0;

            // Групуємо по етапах та сумуємо потужності
            var capacitiesByStage = stations
                .GroupBy(s => s.ProductionStage)
                .Select(g => new
                {
                    Stage = g.Key,
                    TotalCapacity = g.Sum(s => s.Capacity)
                })
                .ToList();

            if (!capacitiesByStage.Any())
                return 0;

            // Потужність цеху = мінімум серед усіх етапів
            return (int)capacitiesByStage.Min(c => c.TotalCapacity);
        }

        /// <summary>
        /// Отримати деталізацію потужності по етапах для цеху
        /// </summary>
        public async Task<Dictionary<ProductionStage, decimal>> GetCapacityByStageAsync(int workshopNumber)
        {
            var stations = await _context.Workstations
                .Where(w => w.WorkshopNumber == workshopNumber && w.IsActive && w.Capacity > 0)
                .ToListAsync();

            return stations
                .GroupBy(s => (ProductionStage)s.ProductionStage)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.Capacity));
        }

        #endregion
    }
}
