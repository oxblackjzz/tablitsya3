using System.Text.Json;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для створення бекапів та відновлення даних
    /// </summary>
    public class BackupService
    {
        private readonly UnifiedStorageService _storageService;
        private readonly ILogger<BackupService> _logger;
        
        // Зберігаємо останні N бекапів в пам'яті
        private readonly List<BackupEntry> _backups = new();
        private const int MaxBackups = 10;

        public BackupService(UnifiedStorageService storageService, ILogger<BackupService> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        /// <summary>
        /// Створює бекап поточних даних
        /// </summary>
        public async Task<BackupEntry> CreateBackupAsync(string reason = "Manual backup")
        {
            try
            {
                var data = await _storageService.LoadWorkshopDataAsync();
                if (data == null)
                {
                    throw new InvalidOperationException("No data to backup");
                }

                var backup = new BackupEntry
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    Reason = reason,
                    Data = CloneData(data),
                    OrderCount = data.WorkshopOrders.Values.Sum(o => o.Count)
                };

                _backups.Insert(0, backup);

                // Видаляємо старі бекапи
                while (_backups.Count > MaxBackups)
                {
                    _backups.RemoveAt(_backups.Count - 1);
                }

                _logger.LogInformation("✅ Backup created: {Reason}, Orders: {Count}", reason, backup.OrderCount);
                
                return backup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create backup");
                throw;
            }
        }

        /// <summary>
        /// Відновлює дані з бекапу
        /// </summary>
        public async Task<bool> RestoreBackupAsync(Guid backupId)
        {
            try
            {
                var backup = _backups.FirstOrDefault(b => b.Id == backupId);
                if (backup == null)
                {
                    _logger.LogWarning("⚠️ Backup not found: {Id}", backupId);
                    return false;
                }

                // Створюємо бекап перед відновленням
                await CreateBackupAsync($"Auto-backup before restore from {backup.CreatedAt:dd.MM.yyyy HH:mm}");

                // Відновлюємо дані
                await _storageService.SaveWorkshopDataAsync(backup.Data);

                _logger.LogInformation("✅ Data restored from backup: {Id}", backupId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to restore backup: {Id}", backupId);
                return false;
            }
        }

        /// <summary>
        /// Отримує список всіх бекапів
        /// </summary>
        public List<BackupEntry> GetBackups()
        {
            return _backups.ToList();
        }

        /// <summary>
        /// Видаляє бекап
        /// </summary>
        public bool DeleteBackup(Guid backupId)
        {
            var backup = _backups.FirstOrDefault(b => b.Id == backupId);
            if (backup != null)
            {
                _backups.Remove(backup);
                _logger.LogInformation("🗑️ Backup deleted: {Id}", backupId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Експортує бекап у JSON
        /// </summary>
        public string ExportToJson(Guid backupId)
        {
            var backup = _backups.FirstOrDefault(b => b.Id == backupId);
            if (backup == null)
            {
                throw new InvalidOperationException("Backup not found");
            }

            return JsonSerializer.Serialize(backup.Data, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        /// <summary>
        /// Експортує поточні дані у JSON
        /// </summary>
        public async Task<string> ExportCurrentDataToJsonAsync()
        {
            var data = await _storageService.LoadWorkshopDataAsync();
            if (data == null)
            {
                throw new InvalidOperationException("No data to export");
            }

            return JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        /// <summary>
        /// Імпортує дані з JSON
        /// </summary>
        public async Task<bool> ImportFromJsonAsync(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<WorkshopData>(json);
                if (data == null)
                {
                    _logger.LogWarning("⚠️ Failed to parse JSON");
                    return false;
                }

                // Створюємо бекап перед імпортом
                await CreateBackupAsync("Auto-backup before import");

                await _storageService.SaveWorkshopDataAsync(data);
                _logger.LogInformation("✅ Data imported from JSON");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to import from JSON");
                return false;
            }
        }

        /// <summary>
        /// Клонує дані для бекапу
        /// </summary>
        private WorkshopData CloneData(WorkshopData source)
        {
            var json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<WorkshopData>(json) ?? new WorkshopData();
        }
    }

    /// <summary>
    /// Запис бекапу
    /// </summary>
    public class BackupEntry
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public WorkshopData Data { get; set; } = new();
        public int OrderCount { get; set; }
    }
}
