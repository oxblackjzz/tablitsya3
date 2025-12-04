using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Універсальний сервіс що автоматично вибирає БД або файлове сховище
    /// </summary>
    public class UnifiedStorageService
    {
   private readonly DatabaseStorageService? _dbStorage;
   private readonly DataStorageService? _fileStorage;
private readonly ILogger<UnifiedStorageService> _logger;

   public UnifiedStorageService(
      IServiceProvider serviceProvider,
            ILogger<UnifiedStorageService> logger)
        {
 _logger = logger;
       
   // Пробуємо отримати DatabaseStorageService
       _dbStorage = serviceProvider.GetService(typeof(DatabaseStorageService)) as DatabaseStorageService;
    
            // Fallback до DataStorageService
    if (_dbStorage == null)
 {
    _fileStorage = serviceProvider.GetService(typeof(DataStorageService)) as DataStorageService;
      }

   var storageType = _dbStorage != null ? "DATABASE" : "FILE";
            _logger.LogInformation($"UnifiedStorageService initialized with {storageType} storage");
        }

   public async Task SaveWorkshopDataAsync(WorkshopData data)
        {
       if (_dbStorage != null)
 {
       _logger.LogDebug("Saving to database...");
    await _dbStorage.SaveWorkshopDataAsync(data);
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
            if (_dbStorage != null)
 {
     _logger.LogDebug("Loading from database...");
         return await _dbStorage.LoadWorkshopDataAsync();
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
 if (_dbStorage != null)
  {
       await _dbStorage.ClearAllDataAsync();
            }
else if (_fileStorage != null)
     {
       await _fileStorage.ClearAllDataAsync();
}
   }

        public async Task<bool> HasSavedDataAsync()
        {
     if (_dbStorage != null)
     {
  return await _dbStorage.HasSavedDataAsync();
}
            else if (_fileStorage != null)
{
          return _fileStorage.HasSavedData();
  }

  return false;
        }

        public bool IsUsingDatabase()
        {
       return _dbStorage != null;
        }

        public string GetStorageType()
  {
            return _dbStorage != null ? "PostgreSQL Database" : "JSON File";
        }
    }
}
