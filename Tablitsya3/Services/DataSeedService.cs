using Tablitsya3.Models;
using System.Text.Json;

namespace Tablitsya3.Services
{
    public class DataSeedService
    {
        private readonly DataStorageService _storage;
        private readonly LoggingService _logger;

        public DataSeedService(DataStorageService storage, LoggingService logger)
        {
    _storage = storage;
     _logger = logger;
}

 public async Task SeedInitialDataIfEmpty()
   {
    try
    {
        var existingData = await _storage.LoadWorkshopDataAsync();
        
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
     StartDate = new DateTime(2025, 1, 1),
        ProductionLeadTime = 5,
 DaysBeforeProduction = 3
        };

  // Initialize capacities
        workshopData.WorkshopCapacities[1] = 1000;
        workshopData.WorkshopCapacities[3] = 1000;
   workshopData.WorkshopCapacities[6] = 1000;

        // Add Workshop 1 orders (from backup)
      var workshop1Orders = new List<(int sqm, DateTime date, string name)>
        {
   (1241, new DateTime(2025, 10, 14), "14.10"),
            (1184, new DateTime(2025, 10, 15), "15.10"),
   (1386, new DateTime(2025, 10, 16), "16.10"),
  (1105, new DateTime(2025, 10, 17), "17.10"),
    (1724, new DateTime(2025, 10, 20), "20.10"),
            (1159, new DateTime(2025, 10, 21), "21.10"),
            (746, new DateTime(2025, 10, 22), "22.10"),
            (897, new DateTime(2025, 10, 23), "23.10"),
         (951, new DateTime(2025, 10, 24), "24.10")
        };

        foreach (var (sqm, date, name) in workshop1Orders)
        {
            workshopData.WorkshopOrders[1].Add(sqm);
            workshopData.WorkshopOrderDates[1].Add(date);
      workshopData.WorkshopOrderNames[1].Add(name);
      }

        // Add custom completion date from backup
workshopData.CustomCompletionDates["1_3"] = new DateTime(2025, 12, 5, 0, 0, 0, DateTimeKind.Local);

      // Save to storage
        await _storage.SaveWorkshopDataAsync(workshopData);

        _logger.LogInfo($"Successfully seeded {workshop1Orders.Count} orders for Workshop 1", "DataSeedService");
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to seed initial data", ex, "DataSeedService");
    }
}
    }
}
