using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Універсальний сервіс що автоматично вибирає БД або файлове сховище
    /// </summary>
    public class UnifiedStorageService
    {
        private readonly IServiceScopeFactory? _scopeFactory;
        private readonly DataStorageService? _fileStorage;
        private readonly ILogger<UnifiedStorageService> _logger;
        private readonly bool _useDatabase;

        public UnifiedStorageService(
          IServiceProvider serviceProvider,
            ILogger<UnifiedStorageService> logger)
    {
       _logger = logger;
    
            // Перевіряємо чи зареєстрований DatabaseStorageService
            using (var scope = serviceProvider.CreateScope())
{
      var dbStorage = scope.ServiceProvider.GetService<DatabaseStorageService>();
            _useDatabase = dbStorage != null;
            }

    if (_useDatabase)
{
_scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
             _logger.LogInformation("UnifiedStorageService initialized with DATABASE storage");
            }
            else
   {
          _fileStorage = serviceProvider.GetService<DataStorageService>();
    _logger.LogInformation("UnifiedStorageService initialized with FILE storage");
            }
        }

        public async Task SaveWorkshopDataAsync(WorkshopData data)
        {
  if (_useDatabase && _scopeFactory != null)
 {
       _logger.LogDebug("Saving to database...");
       using var scope = _scopeFactory.CreateScope();
     var dbStorage = scope.ServiceProvider.GetRequiredService<DatabaseStorageService>();
          await dbStorage.SaveWorkshopDataAsync(data);
    }
            else if (_fileStorage != null)
      {
   _logger.LogDebug("Saving to file...");
   await _fileStorage.SaveWorkshopDataAsync(data);
            }
       else
            {
          throw new InvalidOperationException("No storage service available");
 }
        }

        public async Task<WorkshopData?> LoadWorkshopDataAsync()
        {
            if (_useDatabase && _scopeFactory != null)
            {
 _logger.LogDebug("Loading from database...");
                using var scope = _scopeFactory.CreateScope();
        var dbStorage = scope.ServiceProvider.GetRequiredService<DatabaseStorageService>();
    return await dbStorage.LoadWorkshopDataAsync();
  }
            else if (_fileStorage != null)
   {
            _logger.LogDebug("Loading from file...");
       return await _fileStorage.LoadWorkshopDataAsync();
     }

        return null;
        }

        public async Task ClearAllDataAsync()
        {
            if (_useDatabase && _scopeFactory != null)
            {
                using var scope = _scopeFactory.CreateScope();
       var dbStorage = scope.ServiceProvider.GetRequiredService<DatabaseStorageService>();
      await dbStorage.ClearAllDataAsync();
      }
            else if (_fileStorage != null)
    {
   await _fileStorage.ClearAllDataAsync();
        }
        }

        public async Task<bool> HasSavedDataAsync()
        {
    if (_useDatabase && _scopeFactory != null)
          {
  using var scope = _scopeFactory.CreateScope();
        var dbStorage = scope.ServiceProvider.GetRequiredService<DatabaseStorageService>();
  return await dbStorage.HasSavedDataAsync();
         }
       else if (_fileStorage != null)
            {
   return _fileStorage.HasSavedData();
  }

            return false;
        }

        public bool IsUsingDatabase()
        {
      return _useDatabase;
    }

        public string GetStorageType()
        {
            return _useDatabase ? "PostgreSQL Database" : "JSON File";
        }
    }
}
