using System;
using System.Collections.Generic;

namespace таблиця3.Models
{
    public class Order
    {
        public int Day { get; set; }
        public string OrderName { get; set; } = string.Empty;
        public double SquareMeters { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime CompletionDate { get; set; }
        public DateTime ProductionStartDate { get; set; }
        public DateTime ProductionEndDate { get; set; }
        public int ProductionDay { get; set; }
        public int ProductionStartDay { get; set; }
        public double ProductionDuration { get; set; }
        public Dictionary<int, double> DailyAllocation { get; set; } = new();
        public int WorkshopNumber { get; set; }

        /// <summary>
        /// Перевіряє чи замовлення зараз в роботі (між датою старту виробництва і датою фінішу)
        /// </summary>
        public bool IsInProduction(DateTime currentDate)
        {
            return currentDate.Date >= ProductionStartDate.Date && 
     currentDate.Date <= ProductionEndDate.Date;
   }

        /// <summary>
        /// Перевіряє чи замовлення завершене
        /// </summary>
        public bool IsCompleted(DateTime currentDate)
        {
     return currentDate.Date > ProductionEndDate.Date;
        }

        /// <summary>
      /// Перевіряє чи замовлення ще не почалося
        /// </summary>
    public bool IsNotStarted(DateTime currentDate)
    {
          return currentDate.Date < ProductionStartDate.Date;
        }

 /// <summary>
      /// Отримати статус замовлення на поточну дату
     /// </summary>
     public string GetStatus(DateTime currentDate)
        {
       if (IsNotStarted(currentDate))
            return "Очікує";
            if (IsInProduction(currentDate))
        return "В роботі";
       if (IsCompleted(currentDate))
                return "Завершено";
   return "Невідомо";
        }
    }
}
