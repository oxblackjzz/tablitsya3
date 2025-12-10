using Tablitsya3.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Tablitsya3.Services
{
    public class DataSeedService
    {
        private readonly IServiceProvider _serviceProvider;
   private readonly LoggingService _logger;

        public DataSeedService(IServiceProvider serviceProvider, LoggingService logger)
 {
 _serviceProvider = serviceProvider;
      _logger = logger;
     }

        public async Task SeedInitialDataIfEmpty()
        {
      try
   {
     WorkshopData? existingData = null;
    
       // Спробуємо завантажити з БД, якщо доступна
     using (var scope = _serviceProvider.CreateScope())
        {
      var dbStorage = scope.ServiceProvider.GetService<DatabaseStorageService>();
       if (dbStorage != null)
       {
       existingData = await dbStorage.LoadWorkshopDataAsync();
            }
            else
          {
 // Fallback до файлової системи
      var fileStorage = scope.ServiceProvider.GetService<DataStorageService>();
        if (fileStorage != null)
  {
       existingData = await fileStorage.LoadWorkshopDataAsync();
    }
   }
     }

       // Check if data already exists
       if (existingData?.WorkshopOrders?.Values.Any(orders => orders.Any()) == true)
       {
           _logger.LogInfo("Data already exists, skipping seed", "DataSeedService");
             return;
       }

          _logger.LogInfo("No existing data found, loading initial data...", "DataSeedService");

            // Create initial workshop data with original backup data
  var workshopData = new WorkshopData
      {
   // ✅ UTC ДАТА
   StartDate = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc)
#pragma warning disable CS0618
        ,ProductionLeadTime = 5,
             DaysBeforeProduction = 3
#pragma warning restore CS0618
       };

      // Initialize capacities
       workshopData.WorkshopCapacities[1] = 1000;
      workshopData.WorkshopCapacities[3] = 1000;
      workshopData.WorkshopCapacities[6] = 1000;

      // Initialize per-workshop planning parameters
      workshopData.WorkshopProductionLeadTimes[1] = 5;
      workshopData.WorkshopProductionLeadTimes[3] = 5;
      workshopData.WorkshopProductionLeadTimes[6] = 5;
      
      workshopData.WorkshopDaysBeforeProduction[1] = 16;
      workshopData.WorkshopDaysBeforeProduction[3] = 16;
      workshopData.WorkshopDaysBeforeProduction[6] = 16;

          // Initialize order collections for Workshop 1
 workshopData.WorkshopOrders[1] = new List<double>();
        workshopData.WorkshopOrderDates[1] = new List<DateTime>();
          workshopData.WorkshopOrderNames[1] = new List<string>();

       // ✅ Add Workshop 1 orders - ВСІ ДАТИ В UTC
          var workshop1Orders = new List<(int sqm, DateTime date, string name)>
   {
    (1241, DateTime.SpecifyKind(new DateTime(2025, 10, 14), DateTimeKind.Utc), "14.10"),
      (1184, DateTime.SpecifyKind(new DateTime(2025, 10, 15), DateTimeKind.Utc), "15.10"),
   (1386, DateTime.SpecifyKind(new DateTime(2025, 10, 16), DateTimeKind.Utc), "16.10"),
(1105, DateTime.SpecifyKind(new DateTime(2025, 10, 17), DateTimeKind.Utc), "17.10"),
      (1724, DateTime.SpecifyKind(new DateTime(2025, 10, 20), DateTimeKind.Utc), "20.10"),
     (1159, DateTime.SpecifyKind(new DateTime(2025, 10, 21), DateTimeKind.Utc), "21.10"),
     (746, DateTime.SpecifyKind(new DateTime(2025, 10, 22), DateTimeKind.Utc), "22.10"),
   (897, DateTime.SpecifyKind(new DateTime(2025, 10, 23), DateTimeKind.Utc), "23.10"),
   (951, DateTime.SpecifyKind(new DateTime(2025, 10, 24), DateTimeKind.Utc), "24.10")
              };

   foreach (var (sqm, date, name) in workshop1Orders)
 {
        workshopData.WorkshopOrders[1].Add(sqm);
 workshopData.WorkshopOrderDates[1].Add(date);
      workshopData.WorkshopOrderNames[1].Add(name);
          }

       // ✅ Add custom completion date - UTC
       workshopData.CustomCompletionDates["1_3"] = DateTime.SpecifyKind(new DateTime(2025, 12, 5), DateTimeKind.Utc);

      // Save to storage (БД або файл)
                using (var scope = _serviceProvider.CreateScope())
       {
            var dbStorage = scope.ServiceProvider.GetService<DatabaseStorageService>();
      if (dbStorage != null)
       {
                 await dbStorage.SaveWorkshopDataAsync(workshopData);
           _logger.LogInfo($"Successfully seeded {workshop1Orders.Count} orders to DATABASE", "DataSeedService");
    }
       else
         {
var fileStorage = scope.ServiceProvider.GetService<DataStorageService>();
   if (fileStorage != null)
     {
       await fileStorage.SaveWorkshopDataAsync(workshopData);
      _logger.LogInfo($"Successfully seeded {workshop1Orders.Count} orders to FILE", "DataSeedService");
        }
       }
         }
            }
     catch (Exception ex)
            {
                _logger.LogError("Failed to seed initial data", ex, "DataSeedService");
            }
   }
    }
}
