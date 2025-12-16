using System;
using System.Collections.Generic;
using System.Linq;

namespace Tablitsya3.Models
{
    public class WorkshopData
    {
        // Старий формат (для сумісності)
        public Dictionary<int, List<double>> WorkshopOrders { get; set; } = new();
        public Dictionary<int, List<DateTime>> WorkshopOrderDates { get; set; } = new();
        public Dictionary<int, List<string>> WorkshopOrderNames { get; set; } = new();
        public Dictionary<string, DateTime> CustomCompletionDates { get; set; } = new();
        
        // Новий формат з OrderData
        public Dictionary<int, List<OrderData>> Orders { get; set; } = new();
        
        // Версія даних для міграції
        public int DataVersion { get; set; } = 1;
        
        public Dictionary<int, int> WorkshopCapacities { get; set; } = new();
        public Dictionary<int, int> WorkshopProductionLeadTimes { get; set; } = new();
        public Dictionary<int, int> WorkshopDaysBeforeProduction { get; set; } = new();
        
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public DateTime StartDate { get; set; } = new DateTime(2025, 1, 1);
        
        [Obsolete("Використовуйте WorkshopCapacities для індивідуальної потужності кожного цеху")]
        public int DailyCapacity { get; set; } = 1000;
        
        [Obsolete("Використовуйте WorkshopProductionLeadTimes для індивідуальних параметрів кожного цеху")]
        public int ProductionLeadTime { get; set; } = 5;
        
        [Obsolete("Використовуйте WorkshopDaysBeforeProduction для індивідуальних параметрів кожного цеху")]
        public int DaysBeforeProduction { get; set; } = 1;

        /// <summary>
        /// Мігрувати зі старого формату в новий
        /// </summary>
        public void MigrateToNewFormat()
        {
            if (DataVersion >= 2) return; // Вже мігровано
            
            foreach (var kvp in WorkshopOrders)
            {
                var workshopNumber = kvp.Key;
                var squares = kvp.Value;
                var dates = WorkshopOrderDates.GetValueOrDefault(workshopNumber) ?? new List<DateTime>();
                var names = WorkshopOrderNames.GetValueOrDefault(workshopNumber) ?? new List<string>();
                
                var orderDataList = new List<OrderData>();
                for (int i = 0; i < squares.Count; i++)
                {
                    var customKey = $"{workshopNumber}_{i + 1}";
                    DateTime? customDate = CustomCompletionDates.ContainsKey(customKey) 
                        ? CustomCompletionDates[customKey] 
                        : null;
                    
                    orderDataList.Add(new OrderData
                    {
                        SquareMeters = squares[i],
                        OrderDate = i < dates.Count ? dates[i] : StartDate.AddDays(i),
                        Name = i < names.Count ? names[i] : "",
                        CustomCompletionDate = customDate
                    });
                }
                
                Orders[workshopNumber] = orderDataList;
            }
            
            DataVersion = 2;
        }

        /// <summary>
        /// Синхронізувати новий формат зі старим (для зворотної сумісності)
        /// </summary>
        public void SyncToOldFormat()
        {
            foreach (var kvp in Orders)
            {
                var workshopNumber = kvp.Key;
                var orderDataList = kvp.Value;
                
                WorkshopOrders[workshopNumber] = orderDataList.Select(o => o.SquareMeters).ToList();
                WorkshopOrderDates[workshopNumber] = orderDataList.Select(o => o.OrderDate).ToList();
                WorkshopOrderNames[workshopNumber] = orderDataList.Select(o => o.Name).ToList();
                
                // Синхронізуємо кастомні дати
                for (int i = 0; i < orderDataList.Count; i++)
                {
                    var customKey = $"{workshopNumber}_{i + 1}";
                    var customDate = orderDataList[i].CustomCompletionDate;
                    if (customDate.HasValue)
                    {
                        CustomCompletionDates[customKey] = customDate.Value;
                    }
                    else
                    {
                        CustomCompletionDates.Remove(customKey);
                    }
                }
            }
        }

        /// <summary>
        /// Отримати замовлення цеху в новому форматі
        /// </summary>
        public List<OrderData> GetOrderData(int workshopNumber)
        {
            if (Orders.ContainsKey(workshopNumber))
                return Orders[workshopNumber];
            
            // Конвертуємо зі старого формату
            if (!WorkshopOrders.ContainsKey(workshopNumber))
                return new List<OrderData>();
            
            var squares = WorkshopOrders[workshopNumber];
            var dates = WorkshopOrderDates.GetValueOrDefault(workshopNumber) ?? new List<DateTime>();
            var names = WorkshopOrderNames.GetValueOrDefault(workshopNumber) ?? new List<string>();
            
            var result = new List<OrderData>();
            for (int i = 0; i < squares.Count; i++)
            {
                var customKey = $"{workshopNumber}_{i + 1}";
                DateTime? customDate = CustomCompletionDates.ContainsKey(customKey) 
                    ? CustomCompletionDates[customKey] 
                    : null;
                
                result.Add(new OrderData
                {
                    SquareMeters = squares[i],
                    OrderDate = i < dates.Count ? dates[i] : StartDate.AddDays(i),
                    Name = i < names.Count ? names[i] : "",
                    CustomCompletionDate = customDate
                });
            }
            
            return result;
        }

        /// <summary>
        /// Знайти замовлення по Id
        /// </summary>
        public (int workshopNumber, int index, OrderData? order) FindOrderById(Guid orderId)
        {
            foreach (var kvp in Orders)
            {
                var index = kvp.Value.FindIndex(o => o.Id == orderId);
                if (index >= 0)
                    return (kvp.Key, index, kvp.Value[index]);
            }
            return (0, -1, null);
        }

        /// <summary>
        /// Отримати всі номери цехів
        /// </summary>
        public IEnumerable<int> GetAllWorkshopNumbers()
        {
            return WorkshopCapacities.Keys
                .Union(WorkshopOrders.Keys)
                .Union(Orders.Keys)
                .Distinct()
                .OrderBy(n => n);
        }

        /// <summary>
        /// Перевірити чи існує цех
        /// </summary>
        public bool HasWorkshop(int workshopNumber)
        {
            return WorkshopCapacities.ContainsKey(workshopNumber) ||
                   WorkshopOrders.ContainsKey(workshopNumber) ||
                   Orders.ContainsKey(workshopNumber);
        }

        /// <summary>
        /// Отримати потужність цеху (з дефолтом)
        /// </summary>
        public int GetCapacity(int workshopNumber, int defaultValue = 1000)
        {
            return WorkshopCapacities.GetValueOrDefault(workshopNumber, defaultValue);
        }

        /// <summary>
        /// Отримати тривалість виробництва для цеху (з дефолтом)
        /// </summary>
        public int GetProductionLeadTime(int workshopNumber, int defaultValue = 5)
        {
            return WorkshopProductionLeadTimes.GetValueOrDefault(workshopNumber, defaultValue);
        }

        /// <summary>
        /// Отримати днів до початку виробництва для цеху (з дефолтом)
        /// </summary>
        public int GetDaysBeforeProduction(int workshopNumber, int defaultValue = 16)
        {
            return WorkshopDaysBeforeProduction.GetValueOrDefault(workshopNumber, defaultValue);
        }

        /// <summary>
        /// Ініціалізувати цех з дефолтними значеннями якщо він не існує
        /// </summary>
        public void EnsureWorkshopExists(int workshopNumber)
        {
            if (!WorkshopCapacities.ContainsKey(workshopNumber))
                WorkshopCapacities[workshopNumber] = 1000;
            
            if (!WorkshopProductionLeadTimes.ContainsKey(workshopNumber))
                WorkshopProductionLeadTimes[workshopNumber] = 5;
            
            if (!WorkshopDaysBeforeProduction.ContainsKey(workshopNumber))
                WorkshopDaysBeforeProduction[workshopNumber] = 16;
            
            if (!WorkshopOrders.ContainsKey(workshopNumber))
                WorkshopOrders[workshopNumber] = new List<double>();
            
            if (!WorkshopOrderDates.ContainsKey(workshopNumber))
                WorkshopOrderDates[workshopNumber] = new List<DateTime>();
            
            if (!WorkshopOrderNames.ContainsKey(workshopNumber))
                WorkshopOrderNames[workshopNumber] = new List<string>();
            
            if (!Orders.ContainsKey(workshopNumber))
                Orders[workshopNumber] = new List<OrderData>();
        }

        /// <summary>
        /// Ініціалізувати дефолтні цехи (1, 3, 6) якщо даних немає
        /// </summary>
        public void EnsureDefaultWorkshops()
        {
            var defaultWorkshops = new[] { 1, 3, 6 };
            foreach (var num in defaultWorkshops)
            {
                EnsureWorkshopExists(num);
            }
        }
    }
}
