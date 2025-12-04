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
        }

         // Оновлюємо основні дані
         entity.LastUpdated = data.LastUpdated;
         entity.StartDate = data.StartDate;
       entity.ProductionLeadTime = data.ProductionLeadTime;
   entity.DaysBeforeProduction = data.DaysBeforeProduction;

        // Видаляємо ВСІ старі записи через колекції
 if (entity.Orders.Any())
        {
      var ordersToRemove = entity.Orders.ToList();
            foreach (var order in ordersToRemove)
    {
       entity.Orders.Remove(order);
       }
 }

        if (entity.WorkshopCapacities.Any())
    {
   var capacitiesToRemove = entity.WorkshopCapacities.ToList();
 foreach (var capacity in capacitiesToRemove)
      {
   entity.WorkshopCapacities.Remove(capacity);
  }
        }

   if (entity.CustomCompletionDates.Any())
 {
    var datesToRemove = entity.CustomCompletionDates.ToList();
     foreach (var date in datesToRemove)
     {
       entity.CustomCompletionDates.Remove(date);
 }
   }

  // Додаємо нові замовлення
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
    entity.Orders.Add(new OrderEntity
 {
    WorkshopNumber = workshopNumber,
    SquareMeters = orders[i],
  OrderDate = i < dates.Count ? dates[i] : DateTime.UtcNow.Date,
     OrderName = i < names.Count ? names[i] : string.Empty
  });
   }
        }

        // Додаємо нові потужності
        foreach (var capacity in data.WorkshopCapacities)
      {
entity.WorkshopCapacities.Add(new WorkshopCapacityEntity
      {
  WorkshopNumber = capacity.Key,
      Capacity = capacity.Value
   });
   }

   // Додаємо нові кастомні дати
        foreach (var customDate in data.CustomCompletionDates)
{
    entity.CustomCompletionDates.Add(new CustomCompletionDateEntity
      {
 OrderKey = customDate.Key,
     CompletionDate = customDate.Value
       });
    }

        // Зберігаємо всі зміни
     await _context.SaveChangesAsync();
        _logger.LogInformation("Workshop data saved to database successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error saving workshop data to database");
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
