using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    public class DatabaseStorageService
    {
   private readonly ApplicationDbContext _context;
      private readonly ILogger<DatabaseStorageService> _logger;

public DatabaseStorageService(ApplicationDbContext context, ILogger<DatabaseStorageService> logger)
        {
            _context = context;
        _logger = logger;
   }

        public async Task SaveWorkshopDataAsync(WorkshopData data)
    {
    // ✅ ТРАНЗАКЦІЯ - якщо щось піде не так, все відкотиться
    await using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        data.LastUpdated = DateTime.UtcNow;

        // Отримуємо або створюємо головний запис
   var entity = await _context.WorkshopData
            .Include(w => w.Orders)
       .Include(w => w.WorkshopCapacities)
     .Include(w => w.CustomCompletionDates)
       .FirstOrDefaultAsync();

        if (entity == null)
        {
      entity = new WorkshopDataEntity();
            _context.WorkshopData.Add(entity);
            await _context.SaveChangesAsync(); // Зберігаємо щоб отримати Id
    }

        // Оновлюємо основні дані
        entity.LastUpdated = data.LastUpdated;
        entity.StartDate = data.StartDate;
     entity.ProductionLeadTime = data.ProductionLeadTime;
entity.DaysBeforeProduction = data.DaysBeforeProduction;

    // ✅ ПАКЕТНЕ ВИДАЛЕННЯ замість циклу
  var oldOrders = entity.Orders.ToList();
        if (oldOrders.Any())
        {
      _context.Orders.RemoveRange(oldOrders);
   }

    var oldCapacities = entity.WorkshopCapacities.ToList();
        if (oldCapacities.Any())
        {
       _context.WorkshopCapacities.RemoveRange(oldCapacities);
    }

        var oldDates = entity.CustomCompletionDates.ToList();
     if (oldDates.Any())
   {
 _context.CustomCompletionDates.RemoveRange(oldDates);
}

   // Додаємо нові замовлення
        var newOrders = new List<OrderEntity>();
        foreach (var workshopPair in data.WorkshopOrders)
        {
    var workshopNumber = workshopPair.Key;
     var orders = workshopPair.Value;
      var dates = data.WorkshopOrderDates.ContainsKey(workshopNumber) 
 ? data.WorkshopOrderDates[workshopNumber] 
      : new List<DateTime>();
 var names = data.WorkshopOrderNames.ContainsKey(workshopNumber) 
  ? data.WorkshopOrderNames[workshopNumber] 
     : new List<string>();

      for (int i = 0; i < orders.Count; i++)
    {
       newOrders.Add(new OrderEntity
          {
        WorkshopNumber = workshopNumber,
    SquareMeters = orders[i],
  OrderDate = i < dates.Count ? dates[i] : DateTime.UtcNow.Date,
       OrderName = i < names.Count ? names[i] : string.Empty
        });
    }
        }
        
        // ✅ ПАКЕТНЕ ДОДАВАННЯ замість AddRange в циклі
        if (newOrders.Any())
     {
      await _context.Orders.AddRangeAsync(newOrders);
        }

   // Додаємо нові потужності
 var newCapacities = data.WorkshopCapacities
            .Select(c => new WorkshopCapacityEntity
            {
      WorkshopNumber = c.Key,
    Capacity = c.Value
            })
          .ToList();
        
        if (newCapacities.Any())
   {
      await _context.WorkshopCapacities.AddRangeAsync(newCapacities);
        }

        // Додаємо нові кастомні дати
        var newCustomDates = data.CustomCompletionDates
     .Select(cd => new CustomCompletionDateEntity
     {
       OrderKey = cd.Key,
        CompletionDate = cd.Value
          })
      .ToList();
        
        if (newCustomDates.Any())
    {
            await _context.CustomCompletionDates.AddRangeAsync(newCustomDates);
   }

        // Зберігаємо всі зміни
        await _context.SaveChangesAsync();
        
// ✅ COMMIT ТРАНЗАКЦІЇ - все пройшло успішно
        await transaction.CommitAsync();
        
        _logger.LogInformation($"✅ Workshop data saved: {newOrders.Count} orders, {newCapacities.Count} capacities");
    }
    catch (Exception ex)
    {
     // ✅ ROLLBACK - відкат всіх змін при помилці
        await transaction.RollbackAsync();
        _logger.LogError(ex, "❌ Error saving workshop data - transaction rolled back");
    throw;
    }
}
        public async Task<WorkshopData?> LoadWorkshopDataAsync()
        {
     try
         {
  var entity = await _context.WorkshopData
            .Include(w => w.Orders)
.Include(w => w.WorkshopCapacities)
    .Include(w => w.CustomCompletionDates)
          .FirstOrDefaultAsync();

   if (entity == null)
          {
           _logger.LogInformation("No data found in database");
   return null;
     }

        var data = new WorkshopData
          {
          LastUpdated = entity.LastUpdated,
      StartDate = entity.StartDate,
        ProductionLeadTime = entity.ProductionLeadTime,
    DaysBeforeProduction = entity.DaysBeforeProduction
     };

     // Конвертуємо замовлення
     var ordersByWorkshop = entity.Orders
      .GroupBy(o => o.WorkshopNumber)
      .ToDictionary(g => g.Key, g => g.OrderBy(o => o.OrderDate).ToList());

             foreach (var workshopPair in ordersByWorkshop)
        {
    data.WorkshopOrders[workshopPair.Key] = workshopPair.Value.Select(o => o.SquareMeters).ToList();
  data.WorkshopOrderDates[workshopPair.Key] = workshopPair.Value.Select(o => o.OrderDate).ToList();
   data.WorkshopOrderNames[workshopPair.Key] = workshopPair.Value.Select(o => o.OrderName).ToList();
       }

     // Конвертуємо потужності
       foreach (var capacity in entity.WorkshopCapacities)
   {
                data.WorkshopCapacities[capacity.WorkshopNumber] = capacity.Capacity;
     }

    // Конвертуємо кастомні дати
       foreach (var customDate in entity.CustomCompletionDates)
                {
           data.CustomCompletionDates[customDate.OrderKey] = customDate.CompletionDate;
      }

        _logger.LogInformation($"Workshop data loaded from database. Orders count: {entity.Orders.Count}");
            return data;
            }
    catch (Exception ex)
            {
    _logger.LogError(ex, "Error loading workshop data from database");
     return null;
            }
        }

      public async Task ClearAllDataAsync()
    {
   try
            {
         var entity = await _context.WorkshopData.FirstOrDefaultAsync();
   if (entity != null)
       {
     _context.WorkshopData.Remove(entity);
      await _context.SaveChangesAsync();
    _logger.LogInformation("All workshop data cleared from database");
      }
    }
       catch (Exception ex)
        {
         _logger.LogError(ex, "Error clearing workshop data from database");
       throw;
       }
  }

        public async Task<bool> HasSavedDataAsync()
{
     return await _context.WorkshopData.AnyAsync();
        }

        public async Task MigrateFromJsonAsync(WorkshopData jsonData)
        {
       try
     {
     _logger.LogInformation("Starting migration from JSON to database...");
            await SaveWorkshopDataAsync(jsonData);
        _logger.LogInformation("Migration completed successfully");
            }
      catch (Exception ex)
            {
      _logger.LogError(ex, "Error during migration from JSON");
 throw;
     }
        }
    }
}
