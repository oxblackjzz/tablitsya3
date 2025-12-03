using System;
using System.Collections.Generic;

namespace Tablitsya3.Services
{
    public class WorkingDaysService
    {
      private readonly HashSet<DateTime> _holidays;

   public WorkingDaysService()
      {
_holidays = new HashSet<DateTime>();
            InitializeHolidays2025();
        }

        private void InitializeHolidays2025()
        {
     // Ukrainian holidays 2025
      _holidays.Add(new DateTime(2025, 1, 1)); // New Year
         _holidays.Add(new DateTime(2025, 1, 7));   // Christmas
            _holidays.Add(new DateTime(2025, 3, 8));   // Women's Day
            _holidays.Add(new DateTime(2025, 4, 20));  // Easter
            _holidays.Add(new DateTime(2025, 4, 21));  // Easter Monday
   _holidays.Add(new DateTime(2025, 6, 8));   // Trinity
     _holidays.Add(new DateTime(2025, 6, 28));  // Constitution Day
 _holidays.Add(new DateTime(2025, 8, 24));  // Independence Day
            _holidays.Add(new DateTime(2025, 10, 14)); // Defenders Day
     _holidays.Add(new DateTime(2025, 12, 25)); // Christmas (Catholic)
    }

        public bool IsWorkingDay(DateTime date)
        {
      if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
      return false;

if (_holidays.Contains(date.Date))
          return false;

            return true;
        }

 public DateTime GetNextWorkingDay(DateTime date)
     {
     var nextDay = date.AddDays(1);
            while (!IsWorkingDay(nextDay))
{
        nextDay = nextDay.AddDays(1);
 }
            return nextDay;
        }

        public DateTime AddWorkingDays(DateTime startDate, int workingDays)
        {
            if (workingDays == 0)
                return startDate;

      var currentDate = startDate;

            // Handle negative working days (subtract working days)
     if (workingDays < 0)
         {
   var daysToSubtract = Math.Abs(workingDays);
      var daysSubtracted = 0;

                while (daysSubtracted < daysToSubtract)
           {
    currentDate = GetPreviousWorkingDay(currentDate);
       daysSubtracted++;
                }

           return currentDate;
     }

            // Add working days (positive direction)
            var daysAdded = 0;
    while (daysAdded < workingDays)
            {
     currentDate = GetNextWorkingDay(currentDate);
  daysAdded++;
 }

  return currentDate;
        }

        // Method to get previous working day
    public DateTime GetPreviousWorkingDay(DateTime date)
        {
   var previousDay = date.AddDays(-1);
 while (!IsWorkingDay(previousDay))
    {
     previousDay = previousDay.AddDays(-1);
            }
  return previousDay;
    }

        public int GetWorkingDaysBetween(DateTime startDate, DateTime endDate)
        {
            var workingDays = 0;
       var currentDate = startDate;

         while (currentDate <= endDate)
            {
       if (IsWorkingDay(currentDate))
     workingDays++;
      currentDate = currentDate.AddDays(1);
            }

            return workingDays;
        }

     /// <summary>
      /// Count working days between two dates (excluding start date)
        /// </summary>
        public int CountWorkingDaysBetween(DateTime startDate, DateTime endDate)
        {
       if (endDate <= startDate)
             return 0;

            var workingDays = 0;
      var currentDate = startDate.AddDays(1); // Exclude start date

         while (currentDate <= endDate)
  {
      if (IsWorkingDay(currentDate))
        workingDays++;
              currentDate = currentDate.AddDays(1);
   }

       return workingDays;
        }

        public string GetNonWorkingDayReason(DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Saturday)
        return "Saturday";
         if (date.DayOfWeek == DayOfWeek.Sunday)
     return "Sunday";
    if (_holidays.Contains(date.Date))
    return GetHolidayName(date.Date);

          return "Working day";
}

 private string GetHolidayName(DateTime date)
        {
  return date switch
            {
    { Month: 1, Day: 1 } => "New Year",
    { Month: 1, Day: 7 } => "Christmas",
                { Month: 3, Day: 8 } => "International Women's Day",
    { Month: 4, Day: 20 } => "Easter",
{ Month: 4, Day: 21 } => "Easter Monday",
  { Month: 6, Day: 8 } => "Trinity",
    { Month: 6, Day: 28 } => "Constitution Day",
         { Month: 8, Day: 24 } => "Independence Day",
  { Month: 10, Day: 14 } => "Defenders Day",
            { Month: 12, Day: 25 } => "Christmas (Catholic)",
                _ => "Holiday"
            };
        }

        public void AddHoliday(DateTime date)
        {
   _holidays.Add(date.Date);
        }

        public void RemoveHoliday(DateTime date)
        {
    _holidays.Remove(date.Date);
        }
    }
}
