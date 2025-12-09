using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс з кешуванням для швидкого доступу до даних
    /// Зменшує навантаження на базу даних та покращує продуктивність
    /// </summary>
    public class CachedStorageService
    {
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory? _scopeFactory;
        private readonly DataStorageService? _fileStorage;
        private readonly ILogger<CachedStorageService> _logger;
        private readonly bool _useDatabase;
        
        // Ключі для кешування
        private const string CACHE_KEY_WORKSHOP_DATA = "workshop_data";
        private const string CACHE_KEY_HAS_DATA = "has_saved_data";
        
        // Налаштування кешу
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan HasDataCacheExpiration = TimeSpan.FromMinutes(1);

        public CachedStorageService(
            IMemoryCache cache,
            IServiceProvider serviceProvider,
            ILogger<CachedStorageService> logger)
        {
            _cache = cache;
            _logger = logger;

            // Визначаємо який storage використовувати
            using (var scope = serviceProvider.CreateScope())
            {
                var dbStorage = scope.ServiceProvider.GetService<DatabaseStorageService>();
                _useDatabase = dbStorage != null;
            }
            
            if (_useDatabase)
            {
                _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            }
            else
            {
                _fileStorage = serviceProvider.GetService<DataStorageService>();
            }

            _logger.LogInformation(
                "🚀 CachedStorageService initialized with {StorageType} storage", 
                _useDatabase ? "DATABASE" : "FILE");
        }

        /// <summary>
        /// Завантажує дані з кешу або БД/файлу якщо кеш порожній
        /// </summary>
        public async Task<WorkshopData?> LoadWorkshopDataAsync()
        {
            // Спробуємо отримати з кешу
            if (_cache.TryGetValue(CACHE_KEY_WORKSHOP_DATA, out WorkshopData? cachedData))
            {
                _logger.LogDebug("✅ Data loaded from CACHE (fast!)");
                return cachedData;
            }

            _logger.LogDebug("⚠️ Cache miss - loading from storage...");

            // Якщо кеш порожній - завантажуємо з БД/файлу
            WorkshopData? data = null;

            if (_useDatabase)
            {
                using var scope = CreateScope();
                var scopedDbStorage = scope.ServiceProvider.GetRequiredService<DatabaseStorageService>();
                data = await scopedDbStorage.LoadWorkshopDataAsync();
            }
            else if (_fileStorage != null)
            {
                data = await _fileStorage.LoadWorkshopDataAsync();
            }

            // Зберігаємо в кеш якщо дані є
            if (data != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = CacheExpiration,
                    Priority = CacheItemPriority.High
                };

                _cache.Set(CACHE_KEY_WORKSHOP_DATA, data, cacheOptions);
                _logger.LogInformation("💾 Data cached for {Minutes} minutes", CacheExpiration.TotalMinutes);
            }

            return data;
        }

        /// <summary>
        /// Зберігає дані та очищає кеш
        /// </summary>
        public async Task SaveWorkshopDataAsync(WorkshopData data)
        {
            _logger.LogDebug("💾 Saving data and invalidating cache...");

            if (_useDatabase)
            {
                using var scope = CreateScope();
                var scopedDbStorage = scope.ServiceProvider.GetRequiredService<DatabaseStorageService>();
                await scopedDbStorage.SaveWorkshopDataAsync(data);
            }
            else if (_fileStorage != null)
            {
                await _fileStorage.SaveWorkshopDataAsync(data);
            }
            else
            {
                throw new InvalidOperationException("No storage service available");
            }

            // ✅ ВАЖЛИВО: Очищаємо кеш після збереження
            InvalidateCache();
            _logger.LogInformation("🗑️ Cache invalidated after save");
        }

        /// <summary>
        /// Перевіряє чи є збережені дані (з кешуванням)
        /// </summary>
        public async Task<bool> HasSavedDataAsync()
        {
            // Спробуємо отримати з кешу
            if (_cache.TryGetValue(CACHE_KEY_HAS_DATA, out bool cachedResult))
            {
                _logger.LogDebug("✅ HasData result from CACHE: {Result}", cachedResult);
                return cachedResult;
            }

            _logger.LogDebug("⚠️ Cache miss for HasData - checking storage...");

            bool hasData = false;

            if (_useDatabase && _scopeFactory != null)
            {
                using var scope = CreateScope();
                var scopedDbStorage = scope.ServiceProvider.GetRequiredService<DatabaseStorageService>();
                hasData = await scopedDbStorage.HasSavedDataAsync();
            }
            else if (_fileStorage != null)
            {
                // DataStorageService має HasSavedData() а не HasSavedDataAsync()
                hasData = _fileStorage.HasSavedData();
            }

            // Кешуємо результат на коротший час
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = HasDataCacheExpiration,
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(CACHE_KEY_HAS_DATA, hasData, cacheOptions);
            _logger.LogDebug("💾 HasData result cached: {Result}", hasData);

            return hasData;
        }

        /// <summary>
        /// Очищає всі дані та кеш
        /// </summary>
        public async Task ClearAllDataAsync()
        {
            _logger.LogWarning("🗑️ Clearing all data and cache...");

            if (_useDatabase)
            {
                using var scope = CreateScope();
                var scopedDbStorage = scope.ServiceProvider.GetRequiredService<DatabaseStorageService>();
                await scopedDbStorage.ClearAllDataAsync();
            }
            else if (_fileStorage != null)
            {
                await _fileStorage.ClearAllDataAsync();
            }

            InvalidateCache();
            _logger.LogInformation("✅ All data and cache cleared");
        }

        /// <summary>
        /// Очищає весь кеш вручну
        /// </summary>
        public void InvalidateCache()
        {
            _cache.Remove(CACHE_KEY_WORKSHOP_DATA);
            _cache.Remove(CACHE_KEY_HAS_DATA);
            _logger.LogDebug("🔄 Cache invalidated manually");
        }

        /// <summary>
        /// Отримує статистику кешу
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            var hasWorkshopData = _cache.TryGetValue(CACHE_KEY_WORKSHOP_DATA, out _);
            var hasDataFlag = _cache.TryGetValue(CACHE_KEY_HAS_DATA, out _);

            return new CacheStatistics
            {
                WorkshopDataCached = hasWorkshopData,
                HasDataFlagCached = hasDataFlag,
                CacheExpiration = CacheExpiration
            };
        }

        // Helper method для створення scope (бо DbContext scoped)
        private IServiceScope CreateScope()
        {
            if (_scopeFactory == null)
            {
                throw new InvalidOperationException("ServiceScopeFactory not available");
            }
            return _scopeFactory.CreateScope();
        }
    }

    /// <summary>
    /// Статистика роботи кешу
    /// </summary>
    public class CacheStatistics
    {
        public bool WorkshopDataCached { get; set; }
        public bool HasDataFlagCached { get; set; }
        public TimeSpan CacheExpiration { get; set; }
    }
}
