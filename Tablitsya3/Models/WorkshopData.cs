using System;
using System.Collections.Generic;
using System.Linq;

namespace Tablitsya3.Models
{
    public class WorkshopData
    {
        public Dictionary<int, List<double>> WorkshopOrders { get; set; } = new();
        public Dictionary<int, List<DateTime>> WorkshopOrderDates { get; set; } = new();
        public Dictionary<int, List<string>> WorkshopOrderNames { get; set; } = new();
        public Dictionary<string, DateTime> CustomCompletionDates { get; set; } = new();
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
        /// Отримати всі номери цехів
        /// </summary>
        public IEnumerable<int> GetAllWorkshopNumbers()
        {
            return WorkshopCapacities.Keys
                .Union(WorkshopOrders.Keys)
                .Distinct()
                .OrderBy(n => n);
        }

        /// <summary>
        /// Перевірити чи існує цех
        /// </summary>
        public bool HasWorkshop(int workshopNumber)
        {
            return WorkshopCapacities.ContainsKey(workshopNumber) ||
                   WorkshopOrders.ContainsKey(workshopNumber);
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
