using System;
using System.Collections.Generic;
using System.Linq;
using таблиця3.Models;

namespace таблиця3.Services
{
    public class ProductionPlanningService
    {
   private readonly WorkingDaysService _workingDaysService;

    public ProductionPlanningService(WorkingDaysService workingDaysService)
        {
 _workingDaysService = workingDaysService;
        }

        public ProductionSchedule CalculateSchedule(
            List<double> dailyOrders,
         DateTime startDate,
    int dailyCapacity = 1000,
            int productionLeadTime = 5,
    List<DateTime>? orderDates = null,
   List<string>? orderNames = null,
         Dictionary<string, DateTime>? customCompletionDates = null,
          int workshopNumber = 0,
  int daysBeforeProduction = 1)
        {
          var schedule = new ProductionSchedule
     {
        DailyCapacity = dailyCapacity,
       ProductionLeadTime = productionLeadTime
        };

    var productionDateLoad = new Dictionary<DateTime, double>();

   Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] ========== ПОЧАТОК РОЗРАХУНКУ ==========");
            Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] Замовлень: {dailyOrders.Count}, Потужність: {dailyCapacity} м?/день");

        for (int day = 1; day <= dailyOrders.Count; day++)
            {
          var orderSize = dailyOrders[day - 1];

          DateTime orderDate;
              if (orderDates != null && orderDates.Count >= day)
  {
        orderDate = orderDates[day - 1];
             }
    else
                {
    orderDate = startDate.AddDays(day - 1);
    }

    string orderName = string.Empty;
   if (orderNames != null && orderNames.Count >= day)
        {
    orderName = orderNames[day - 1];
      }

  var customKey = $"{workshopNumber}_{day}";
                DateTime? customCompletionDate = null;
 if (customCompletionDates != null && customCompletionDates.ContainsKey(customKey))
    {
              customCompletionDate = customCompletionDates[customKey];
             }

        // ? НОВИЙ РОЗРАХУНОК СТАРТУ (v3):
          // 1. Базовий старт: мінімум +1 робочий день від замовлення
     var earliestPossibleStart = _workingDaysService.AddWorkingDays(orderDate, daysBeforeProduction);
        
      // 2. Шукаємо перший день, коли цех може прийняти замовлення
  DateTime actualProductionStart = FindNextAvailableStartDate(
         earliestPossibleStart, 
      orderSize, 
          dailyCapacity, 
      productionDateLoad,
       workshopNumber);

          if (actualProductionStart > earliestPossibleStart)
     {
                  var delayDays = _workingDaysService.CountWorkingDaysBetween(earliestPossibleStart, actualProductionStart);
  Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] Замовлення #{day}: Відкладено старт на {delayDays} робочих днів (з {earliestPossibleStart:dd.MM.yyyy} на {actualProductionStart:dd.MM.yyyy}) через завантаженість цеху");
              }

        Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] Замовлення #{day}: Обсяг={orderSize} м?, Замовлено={orderDate:dd.MM.yyyy}, Старт={actualProductionStart:dd.MM.yyyy}");

    var remainingOrder = orderSize;
            var currentDate = actualProductionStart;
      var productionStartDate = currentDate;
       DateTime productionEndDate = currentDate;

   var dailyAllocation = new Dictionary<int, double>();
     int dayIndex = 1;
                double totalCapacityOverflow = 0;

          // Якщо є кастомна дата відвантаження, працюємо в зворотному напрямку
     if (customCompletionDate.HasValue)
       {
        var targetEndDate = _workingDaysService.AddWorkingDays(customCompletionDate.Value, -(productionLeadTime + 1));
         currentDate = actualProductionStart;

        while (remainingOrder > 0 && currentDate <= targetEndDate)
          {
      while (!_workingDaysService.IsWorkingDay(currentDate) && currentDate <= targetEndDate)
           {
       currentDate = currentDate.AddDays(1);
          }

if (currentDate > targetEndDate)
       break;

         if (!productionDateLoad.ContainsKey(currentDate))
         {
   productionDateLoad[currentDate] = 0;
        }

          var availableCapacity = dailyCapacity - productionDateLoad[currentDate];

       if (availableCapacity > 0)
         {
         var toAllocate = Math.Min(remainingOrder, availableCapacity);
     productionDateLoad[currentDate] += toAllocate;
   dailyAllocation[dayIndex] = toAllocate;
     remainingOrder -= toAllocate;
        productionEndDate = currentDate;

     var overflow = productionDateLoad[currentDate] - dailyCapacity;
   if (overflow > 0)
        {
        totalCapacityOverflow += overflow;
 }

         Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}]   День {currentDate:dd.MM.yyyy}: виділено {toAllocate:F0} м?, залишилось {remainingOrder:F0} м?, завантаженість цеху: {productionDateLoad[currentDate]:F0}/{dailyCapacity}");

       dayIndex++;
    }

  currentDate = currentDate.AddDays(1);
     }

         while (remainingOrder > 0)
      {
      while (!_workingDaysService.IsWorkingDay(currentDate))
        {
      currentDate = currentDate.AddDays(1);
           }

   if (!productionDateLoad.ContainsKey(currentDate))
       {
    productionDateLoad[currentDate] = 0;
  }

           var availableCapacity = dailyCapacity - productionDateLoad[currentDate];

             if (availableCapacity > 0)
          {
       var toAllocate = Math.Min(remainingOrder, availableCapacity);
productionDateLoad[currentDate] += toAllocate;
       dailyAllocation[dayIndex] = toAllocate;
          remainingOrder -= toAllocate;
          productionEndDate = currentDate;

 var overflow = productionDateLoad[currentDate] - dailyCapacity;
           if (overflow > 0)
     {
          totalCapacityOverflow += overflow;
         }

               Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}]   День {currentDate:dd.MM.yyyy}: виділено {toAllocate:F0} м?, залишилось {remainingOrder:F0} м?, завантаженість цеху: {productionDateLoad[currentDate]:F0}/{dailyCapacity}");

        dayIndex++;
 }

       currentDate = currentDate.AddDays(1);
      }
     }
        else
       {
   // Стандартний розподіл без кастомної дати
    while (remainingOrder > 0)
          {
      while (!_workingDaysService.IsWorkingDay(currentDate))
      {
         currentDate = currentDate.AddDays(1);
          }

          if (!productionDateLoad.ContainsKey(currentDate))
             {
  productionDateLoad[currentDate] = 0;
         }

     var availableCapacity = dailyCapacity - productionDateLoad[currentDate];

   if (availableCapacity > 0)
         {
          var toAllocate = Math.Min(remainingOrder, availableCapacity);
    productionDateLoad[currentDate] += toAllocate;
        dailyAllocation[dayIndex] = toAllocate;
       remainingOrder -= toAllocate;
   productionEndDate = currentDate;

     var overflow = productionDateLoad[currentDate] - dailyCapacity;
   if (overflow > 0)
{
         totalCapacityOverflow += overflow;
 }

               Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}]   День {currentDate:dd.MM.yyyy}: виділено {toAllocate:F0} м?, залишилось {remainingOrder:F0} м?, завантаженість цеху: {productionDateLoad[currentDate]:F0}/{dailyCapacity}");

           dayIndex++;
           }

             currentDate = currentDate.AddDays(1);
        }
}

         var productionDuration = orderSize / dailyCapacity;
      var workingDaysUsed = dailyAllocation.Count;

    // РОЗРАХУНОК ФІНІШУ: +5 робочих днів + пропорційні дні при перевищенні
 DateTime calculatedFinish = productionEndDate;

                if (!customCompletionDate.HasValue)
   {
        calculatedFinish = _workingDaysService.AddWorkingDays(productionEndDate, productionLeadTime);

     if (totalCapacityOverflow > 0)
 {
 var additionalDays = (int)Math.Ceiling(totalCapacityOverflow / dailyCapacity);

    Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] УВАГА: Перевищення потужності на {totalCapacityOverflow:F0} м?!");
       Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}]   Додається {additionalDays} днів до фінішу");

   calculatedFinish = calculatedFinish.AddDays(additionalDays);
           }

     productionEndDate = calculatedFinish;
        }

   // ВІДВАНТАЖЕННЯ: наступний РОБОЧИЙ день після фінішу
          var completionDate = _workingDaysService.GetNextWorkingDay(productionEndDate);

   var productionStartDayNumber = (int)(productionStartDate - startDate).TotalDays + 1;
        var productionEndDayNumber = (int)(productionEndDate - startDate).TotalDays + 1;

    Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] Замовлення #{day}: Виробництво {productionStartDate:dd.MM.yyyy} - {productionEndDate:dd.MM.yyyy}, Відвантаження {completionDate:dd.MM.yyyy}");
     Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}]   Перевищення потужності: {totalCapacityOverflow:F0} м? ({(totalCapacityOverflow > 0 ? "ТАК" : "НІ")})");

    var order = new Order
          {
   Day = day,
         OrderName = orderName,
         SquareMeters = orderSize,
  StartDate = orderDate,
         CompletionDate = completionDate,
      ProductionStartDate = productionStartDate,
         ProductionEndDate = productionEndDate,
    ProductionDay = productionEndDayNumber,
      ProductionStartDay = productionStartDayNumber,
  ProductionDuration = productionDuration,
         DailyAllocation = dailyAllocation
          };

 schedule.Orders.Add(order);
            }

            Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] ========== КІНЕЦЬ РОЗРАХУНКУ ==========");

 var dailyLoad = new Dictionary<int, double>();
            foreach (var kvp in productionDateLoad.OrderBy(x => x.Key))
      {
      var dayIndex = (int)(kvp.Key - startDate).TotalDays + 1;
         dailyLoad[dayIndex] = kvp.Value;
  }
       schedule.DailyLoad = dailyLoad;

            return schedule;
   }

        /// <summary>
      /// Знаходить перший доступний день для початку виробництва з урахуванням завантаженості цеху
      /// </summary>
        private DateTime FindNextAvailableStartDate(
   DateTime earliestStart, 
 double orderSize, 
            int dailyCapacity, 
          Dictionary<DateTime, double> productionDateLoad,
   int workshopNumber)
      {
     var candidateDate = earliestStart;
            var remainingToAllocate = orderSize;

    // Симулюємо розподіл замовлення, щоб знайти перший день, коли воно може почати виконуватися
 while (remainingToAllocate > 0)
            {
     // Пропускаємо неробочі дні
      while (!_workingDaysService.IsWorkingDay(candidateDate))
                {
  candidateDate = candidateDate.AddDays(1);
}

     // Перевіряємо завантаженість на цю дату
   var currentLoad = productionDateLoad.ContainsKey(candidateDate) 
           ? productionDateLoad[candidateDate] 
          : 0;

  var availableCapacity = dailyCapacity - currentLoad;

                if (availableCapacity > 0)
   {
    // Цех має вільну потужність - це наш старт!
    return candidateDate;
        }

 // Цех повністю завантажений - пробуємо наступний день
         candidateDate = candidateDate.AddDays(1);
 }

            return candidateDate;
      }

        public Dictionary<int, double> GetCapacityUtilization(ProductionSchedule schedule)
        {
    var utilization = new Dictionary<int, double>();

  foreach (var kvp in schedule.DailyLoad)
  {
      utilization[kvp.Key] = (kvp.Value / schedule.DailyCapacity) * 100;
    }

      return utilization;
        }
    }
}
