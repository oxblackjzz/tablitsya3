using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Універсальний сервіс що автоматично вибирає БД або файлове сховище
    /// ОНОВЛЕНО: Тепер використовує кешування для швидкодії
    /// </summary>
    public class UnifiedStorageService
    {
        private readonly CachedStorageService _cachedStorage;
        private readonly ILogger<UnifiedStorageService> _logger;
        private readonly bool _useDatabase;

        public UnifiedStorageService(
            CachedStorageService cachedStorage,
            IServiceProvider serviceProvider,
            ILogger<UnifiedStorageService> logger)
        {
            _cachedStorage = cachedStorage;
            _logger = logger;
    
            // Перевіряємо чи зареєстрований DatabaseStorageService
            using (var scope = serviceProvider.CreateScope())
            {
                var dbStorage = scope.ServiceProvider.GetService<DatabaseStorageService>();
                _useDatabase = dbStorage != null;
            }

            _logger.LogInformation(
                "✅ UnifiedStorageService initialized with {StorageType} storage + CACHING", 
                _useDatabase ? "DATABASE" : "FILE");
        }

        public async Task SaveWorkshopDataAsync(WorkshopData data)
        {
            // Автоматично синхронізуємо формати перед збереженням
            if (data.DataVersion >= 2)
            {
                data.SyncToOldFormat();
            }
            
            _logger.LogDebug("💾 Saving data through cached storage...");
            await _cachedStorage.SaveWorkshopDataAsync(data);
        }

        public async Task<WorkshopData?> LoadWorkshopDataAsync()
        {
            _logger.LogDebug("📖 Loading data through cached storage...");
            var data = await _cachedStorage.LoadWorkshopDataAsync();
            
            // Автоматична міграція при завантаженні
            if (data != null && data.DataVersion < 2)
            {
                _logger.LogInformation("🔄 Migrating data to new OrderData format...");
                data.MigrateToNewFormat();
                // Зберігаємо мігровані дані
                await _cachedStorage.SaveWorkshopDataAsync(data);
                _logger.LogInformation("✅ Data migration completed");
            }
            
            return data;
        }

        public async Task ClearAllDataAsync()
        {
            _logger.LogWarning("🗑️ Clearing all data through cached storage...");
            await _cachedStorage.ClearAllDataAsync();
        }

        public async Task<bool> HasSavedDataAsync()
        {
            return await _cachedStorage.HasSavedDataAsync();
        }

        public bool IsUsingDatabase()
        {
            return _useDatabase;
        }

        public string GetStorageType()
        {
            return _useDatabase ? "PostgreSQL Database (with cache)" : "JSON File (with cache)";
        }
        
        /// <summary>
        /// Очистити кеш вручну
        /// </summary>
        public void InvalidateCache()
        {
            _cachedStorage.InvalidateCache();
            _logger.LogInformation("🔄 Cache manually invalidated");
        }
        
        /// <summary>
        /// Отримати статистику кешу
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            return _cachedStorage.GetCacheStatistics();
        }
        
        /// <summary>
        /// Примусова міграція даних в новий формат
        /// </summary>
        public async Task ForceMigrateAsync()
        {
            var data = await _cachedStorage.LoadWorkshopDataAsync();
            if (data != null)
            {
                _logger.LogInformation("🔄 Force migrating data to OrderData format...");
                data.MigrateToNewFormat();
                await _cachedStorage.SaveWorkshopDataAsync(data);
                _logger.LogInformation("✅ Force migration completed");
            }
        }
    }
}
