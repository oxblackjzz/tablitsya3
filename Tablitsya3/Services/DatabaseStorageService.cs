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
   .AsTracking() // ✅ Відслідковуємо зміни
       .FirstOrDefaultAsync();

        if (entity == null)
        {
     entity = new WorkshopDataEntity();
  _context.WorkshopData.Add(entity);
     
            // ✅ ЗБЕРІГАЄМО ОДРАЗУ ЩОБ ОТРИМАТИ ID
    await _context.SaveChangesAsync();
    _logger.LogInformation("Created new workshop data entity with ID: {Id}", entity.Id);
   }

    // ✅ КОНВЕРТУЄМО ВСІ ДАТИ В UTC
        entity.LastUpdated = DateTime.SpecifyKind(data.LastUpdated, DateTimeKind.Utc);
  entity.StartDate = DateTime.SpecifyKind(data.StartDate.Date, DateTimeKind.Utc);
      entity.ProductionLeadTime = data.ProductionLeadTime;
     entity.DaysBeforeProduction = data.DaysBeforeProduction;

      // ✅ ПАКЕТНЕ ВИДАЛЕННЯ - Крок 1: Видаляємо старі дані
        if (entity.Orders.Any())
        {
         _context.Orders.RemoveRange(entity.Orders.ToList());
        }

        if (entity.WorkshopCapacities.Any())
        {
      _context.WorkshopCapacities.RemoveRange(entity.WorkshopCapacities.ToList());
        }

    if (entity.CustomCompletionDates.Any())
        {
   _context.CustomCompletionDates.RemoveRange(entity.CustomCompletionDates.ToList());
    }

        // ✅ ЗБЕРІГАЄМО ВИДАЛЕННЯ ПЕРЕД ДОДАВАННЯМ НОВИХ
        if (entity.Orders.Any() || entity.WorkshopCapacities.Any() || entity.CustomCompletionDates.Any())
        {
  await _context.SaveChangesAsync();
      _logger.LogInformation("Deleted old data before adding new");
   
  // ✅ ОЧИЩАЄМО КОЛЕКЦІЇ ПІСЛЯ ЗБЕРЕЖЕННЯ
  entity.Orders.Clear();
            entity.WorkshopCapacities.Clear();
  entity.CustomCompletionDates.Clear();
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
     // ✅ КОНВЕРТУЄМО ДАТУ ЗАМОВЛЕННЯ В UTC
   var orderDate = i < dates.Count ? dates[i] : DateTime.UtcNow.Date;
      orderDate = DateTime.SpecifyKind(orderDate.Date, DateTimeKind.Utc);

      newOrders.Add(new OrderEntity
        {
          WorkshopNumber = workshopNumber,
      SquareMeters = orders[i],
   OrderDate = orderDate,
 OrderName = i < names.Count ? names[i] : string.Empty
  });
    }
        }

   // ✅ ДОДАЄМО НОВІ ЗАМОВЛЕННЯ
        if (newOrders.Any())
        {
      foreach (var order in newOrders)
      {
     entity.Orders.Add(order);
   }
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
foreach (var capacity in newCapacities)
   {
          entity.WorkshopCapacities.Add(capacity);
     }
        }

    // Додаємо нові кастомні дати
 var newCustomDates = data.CustomCompletionDates
    .Select(cd =>
        {
       // ✅ КОНВЕРТУЄМО КАСТОМНУ ДАТУ В UTC
        var completionDate = DateTime.SpecifyKind(cd.Value.Date, DateTimeKind.Utc);
         
   return new CustomCompletionDateEntity
      {
    OrderKey = cd.Key,
   CompletionDate = completionDate
    };
       })
 .ToList();
      
  if (newCustomDates.Any())
        {
            foreach (var customDate in newCustomDates)
        {
  entity.CustomCompletionDates.Add(customDate);
  }
    }

     // ✅ ЗБЕРІГАЄМО НОВІ ДАНІ
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

        // ✅ КОНВЕРТУЄМО UTC НАЗАД У LOCAL
   var data = new WorkshopData
        {
     LastUpdated = entity.LastUpdated.ToLocalTime(),
       StartDate = entity.StartDate.ToLocalTime().Date,
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
    
      // ✅ КОНВЕРТУЄМО ДАТИ ЗАМОВЛЕНЬ З UTC В LOCAL
  data.WorkshopOrderDates[workshopPair.Key] = workshopPair.Value
       .Select(o => o.OrderDate.ToLocalTime().Date)
         .ToList();
      
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
      // ✅ КОНВЕРТУЄМО КАСТОМНІ ДАТИ З UTC В LOCAL
      data.CustomCompletionDates[customDate.OrderKey] = customDate.CompletionDate.ToLocalTime().Date;
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
