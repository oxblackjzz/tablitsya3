using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для управління конфігурацією цехів
    /// </summary>
    public class WorkshopConfigService
    {
        private readonly UnifiedStorageService _storageService;
        private readonly ILogger<WorkshopConfigService> _logger;
        
        // Кешовані конфігурації цехів
        private List<WorkshopConfig>? _cachedConfigs;
        private DateTime _cacheTime = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public WorkshopConfigService(
            UnifiedStorageService storageService,
            ILogger<WorkshopConfigService> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        /// <summary>
        /// Отримати всі активні цехи
        /// </summary>
        public async Task<List<WorkshopConfig>> GetActiveWorkshopsAsync()
        {
            var all = await GetAllWorkshopsAsync();
            return all.Where(w => w.IsActive).OrderBy(w => w.SortOrder).ToList();
        }

        /// <summary>
        /// Отримати всі цехи (включаючи неактивні)
        /// </summary>
        public async Task<List<WorkshopConfig>> GetAllWorkshopsAsync()
        {
            // Перевірка кешу
            if (_cachedConfigs != null && DateTime.Now - _cacheTime < _cacheExpiry)
            {
                return _cachedConfigs;
            }

            var data = await _storageService.LoadWorkshopDataAsync();
            if (data == null)
            {
                _cachedConfigs = GetDefaultWorkshops();
            }
            else
            {
                _cachedConfigs = BuildWorkshopConfigsFromData(data);
            }

            _cacheTime = DateTime.Now;
            return _cachedConfigs;
        }

        /// <summary>
        /// Отримати конфігурацію конкретного цеху
        /// </summary>
        public async Task<WorkshopConfig?> GetWorkshopAsync(int workshopNumber)
        {
            var workshops = await GetAllWorkshopsAsync();
            return workshops.FirstOrDefault(w => w.Number == workshopNumber);
        }

        /// <summary>
        /// Отримати номери всіх активних цехів
        /// </summary>
        public async Task<int[]> GetActiveWorkshopNumbersAsync()
        {
            var workshops = await GetActiveWorkshopsAsync();
            return workshops.Select(w => w.Number).ToArray();
        }

        /// <summary>
        /// Додати новий цех
        /// </summary>
        public async Task<WorkshopConfig> AddWorkshopAsync(int number, string? name = null)
        {
            _logger.LogInformation("Adding new workshop #{Number}", number);

            var data = await _storageService.LoadWorkshopDataAsync() ?? new WorkshopData();
            
            // Перевірка чи цех вже існує
            if (data.WorkshopCapacities.ContainsKey(number))
            {
                throw new InvalidOperationException($"Цех №{number} вже існує");
            }

            // Створюємо конфігурацію
            var config = WorkshopConfig.CreateDefault(number);
            if (!string.IsNullOrWhiteSpace(name))
            {
                config.Name = name;
            }

            // Додаємо до даних
            data.WorkshopCapacities[number] = config.Capacity;
            data.WorkshopProductionLeadTimes[number] = config.ProductionLeadTime;
            data.WorkshopDaysBeforeProduction[number] = config.DaysBeforeProduction;
            data.WorkshopOrders[number] = new List<double>();
            data.WorkshopOrderDates[number] = new List<DateTime>();
            data.WorkshopOrderNames[number] = new List<string>();

            await _storageService.SaveWorkshopDataAsync(data);
            
            // Інвалідуємо кеш
            InvalidateCache();

            _logger.LogInformation("Workshop #{Number} '{Name}' added successfully", number, config.Name);
            return config;
        }

        /// <summary>
        /// Оновити конфігурацію цеху
        /// </summary>
        public async Task UpdateWorkshopAsync(WorkshopConfig config)
        {
            _logger.LogInformation("Updating workshop #{Number}", config.Number);

            var data = await _storageService.LoadWorkshopDataAsync() ?? new WorkshopData();

            data.WorkshopCapacities[config.Number] = config.Capacity;
            data.WorkshopProductionLeadTimes[config.Number] = config.ProductionLeadTime;
            data.WorkshopDaysBeforeProduction[config.Number] = config.DaysBeforeProduction;

            await _storageService.SaveWorkshopDataAsync(data);
            InvalidateCache();

            _logger.LogInformation("Workshop #{Number} updated successfully", config.Number);
        }

        /// <summary>
        /// Видалити цех (деактивувати)
        /// </summary>
        public async Task<bool> RemoveWorkshopAsync(int workshopNumber)
        {
            _logger.LogWarning("Removing workshop #{Number}", workshopNumber);

            var data = await _storageService.LoadWorkshopDataAsync();
            if (data == null) return false;

            // Видаляємо дані цеху
            data.WorkshopCapacities.Remove(workshopNumber);
            data.WorkshopProductionLeadTimes.Remove(workshopNumber);
            data.WorkshopDaysBeforeProduction.Remove(workshopNumber);
            data.WorkshopOrders.Remove(workshopNumber);
            data.WorkshopOrderDates.Remove(workshopNumber);
            data.WorkshopOrderNames.Remove(workshopNumber);

            // Видаляємо кастомні дати для цього цеху
            var keysToRemove = data.CustomCompletionDates
                .Where(kvp => kvp.Key.StartsWith($"{workshopNumber}_"))
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                data.CustomCompletionDates.Remove(key);
            }

            await _storageService.SaveWorkshopDataAsync(data);
            InvalidateCache();

            _logger.LogInformation("Workshop #{Number} removed successfully", workshopNumber);
            return true;
        }

        /// <summary>
        /// Інвалідувати кеш
        /// </summary>
        public void InvalidateCache()
        {
            _cachedConfigs = null;
            _cacheTime = DateTime.MinValue;
        }

        /// <summary>
        /// Побудувати конфігурації з WorkshopData
        /// </summary>
        private List<WorkshopConfig> BuildWorkshopConfigsFromData(WorkshopData data)
        {
            var configs = new List<WorkshopConfig>();
            var colors = new[] { "primary", "success", "warning", "info", "danger", "secondary" };

            // Збираємо всі унікальні номери цехів
            var workshopNumbers = data.WorkshopCapacities.Keys
                .Union(data.WorkshopOrders.Keys)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            foreach (var number in workshopNumbers)
            {
                var config = new WorkshopConfig
                {
                    Number = number,
                    Name = $"Цех №{number}",
                    Capacity = data.WorkshopCapacities.GetValueOrDefault(number, 1000),
                    ProductionLeadTime = data.WorkshopProductionLeadTimes.GetValueOrDefault(number, 5),
                    DaysBeforeProduction = data.WorkshopDaysBeforeProduction.GetValueOrDefault(number, 16),
                    ColorClass = colors[(number - 1) % colors.Length],
                    SortOrder = number,
                    IsActive = true
                };
                configs.Add(config);
            }

            // Якщо немає жодного цеху, повертаємо дефолтні
            if (!configs.Any())
            {
                return GetDefaultWorkshops();
            }

            return configs;
        }

        /// <summary>
        /// Отримати цехи за замовчуванням
        /// </summary>
        private List<WorkshopConfig> GetDefaultWorkshops()
        {
            return new List<WorkshopConfig>
            {
                new WorkshopConfig { Number = 1, Name = "Цех №1", ColorClass = "primary", SortOrder = 1 },
                new WorkshopConfig { Number = 3, Name = "Цех №3", ColorClass = "success", SortOrder = 2 },
                new WorkshopConfig { Number = 6, Name = "Цех №6", ColorClass = "warning", SortOrder = 3 }
            };
        }
    }
}
