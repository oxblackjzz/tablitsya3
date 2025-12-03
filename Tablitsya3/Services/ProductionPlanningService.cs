using System;
using System.Collections.Generic;
using System.Linq;
using �������3.Models;

namespace �������3.Services
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

   Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] ========== ������� ���������� ==========");
            Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] ���������: {dailyOrders.Count}, ���������: {dailyCapacity} �?/����");

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

        // ? ����� ���������� ������ (v3):
          // 1. ������� �����: ����� +1 ������� ���� �� ����������
     var earliestPossibleStart = _workingDaysService.AddWorkingDays(orderDate, daysBeforeProduction);
        
      // 2. ������ ������ ����, ���� ��� ���� �������� ����������
  DateTime actualProductionStart = FindNextAvailableStartDate(
         earliestPossibleStart, 
      orderSize, 
          dailyCapacity, 
      productionDateLoad,
       workshopNumber);

          if (actualProductionStart > earliestPossibleStart)
     {
                  var delayDays = _workingDaysService.CountWorkingDaysBetween(earliestPossibleStart, actualProductionStart);
  Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] ���������� #{day}: ³�������� ����� �� {delayDays} ������� ��� (� {earliestPossibleStart:dd.MM.yyyy} �� {actualProductionStart:dd.MM.yyyy}) ����� ������������� ����");
              }

        Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] ���������� #{day}: �����={orderSize} �?, ���������={orderDate:dd.MM.yyyy}, �����={actualProductionStart:dd.MM.yyyy}");

    var remainingOrder = orderSize;
            var currentDate = actualProductionStart;
      var productionStartDate = currentDate;
       DateTime productionEndDate = currentDate;

   var dailyAllocation = new Dictionary<int, double>();
     int dayIndex = 1;
                double totalCapacityOverflow = 0;

          // ���� � �������� ���� ������������, �������� � ���������� ��������
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

         Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}]   ���� {currentDate:dd.MM.yyyy}: ������� {toAllocate:F0} �?, ���������� {remainingOrder:F0} �?, ������������� ����: {productionDateLoad[currentDate]:F0}/{dailyCapacity}");

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

               Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}]   ���� {currentDate:dd.MM.yyyy}: ������� {toAllocate:F0} �?, ���������� {remainingOrder:F0} �?, ������������� ����: {productionDateLoad[currentDate]:F0}/{dailyCapacity}");

        dayIndex++;
 }

       currentDate = currentDate.AddDays(1);
      }
     }
        else
       {
   // ����������� ������� ��� �������� ����
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

               Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}]   ���� {currentDate:dd.MM.yyyy}: ������� {toAllocate:F0} �?, ���������� {remainingOrder:F0} �?, ������������� ����: {productionDateLoad[currentDate]:F0}/{dailyCapacity}");

           dayIndex++;
           }

             currentDate = currentDate.AddDays(1);
        }
}

         var productionDuration = orderSize / dailyCapacity;
      var workingDaysUsed = dailyAllocation.Count;

    // ���������� ԲͲ��: +5 ������� ��� + ���������� �� ��� ����������
 DateTime calculatedFinish = productionEndDate;

                if (!customCompletionDate.HasValue)
   {
        calculatedFinish = _workingDaysService.AddWorkingDays(productionEndDate, productionLeadTime);

     if (totalCapacityOverflow > 0)
 {
 var additionalDays = (int)Math.Ceiling(totalCapacityOverflow / dailyCapacity);

    Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] �����: ����������� ��������� �� {totalCapacityOverflow:F0} �?!");
       Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}]   �������� {additionalDays} ��� �� ����");

   calculatedFinish = calculatedFinish.AddDays(additionalDays);
           }

     productionEndDate = calculatedFinish;
        }

   // ²�����������: ��������� ������� ���� ���� ����
          var completionDate = _workingDaysService.GetNextWorkingDay(productionEndDate);

   var productionStartDayNumber = (int)(productionStartDate - startDate).TotalDays + 1;
        var productionEndDayNumber = (int)(productionEndDate - startDate).TotalDays + 1;

    Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] ���������� #{day}: ����������� {productionStartDate:dd.MM.yyyy} - {productionEndDate:dd.MM.yyyy}, ³����������� {completionDate:dd.MM.yyyy}");
     Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}]   ����������� ���������: {totalCapacityOverflow:F0} �? ({(totalCapacityOverflow > 0 ? "���" : "Ͳ")})");

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

            Console.WriteLine($"[ProductionPlanningService Workshop#{workshopNumber}] ========== ʲ���� ���������� ==========");

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
      /// ��������� ������ ��������� ���� ��� ������� ����������� � ����������� ������������� ����
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

    // ��������� ������� ����������, ��� ������ ������ ����, ���� ���� ���� ������ ������������
 while (remainingToAllocate > 0)
            {
     // ���������� �������� ��
      while (!_workingDaysService.IsWorkingDay(candidateDate))
                {
  candidateDate = candidateDate.AddDays(1);
}

     // ���������� ������������� �� �� ����
   var currentLoad = productionDateLoad.ContainsKey(candidateDate) 
           ? productionDateLoad[candidateDate] 
          : 0;

  var availableCapacity = dailyCapacity - currentLoad;

                if (availableCapacity > 0)
   {
    // ��� �� ����� ��������� - �� ��� �����!
    return candidateDate;
        }

 // ��� ������� ������������ - ������� ��������� ����
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
