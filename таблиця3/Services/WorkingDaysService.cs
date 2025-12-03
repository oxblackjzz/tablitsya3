using System;
using System.Collections.Generic;

namespace таблиця3.Services
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
            _holidays.Add(new DateTime(2025, 1, 1));
            _holidays.Add(new DateTime(2025, 1, 7));
            _holidays.Add(new DateTime(2025, 3, 8));
            _holidays.Add(new DateTime(2025, 4, 20));
            _holidays.Add(new DateTime(2025, 4, 21));
            _holidays.Add(new DateTime(2025, 6, 8));
            _holidays.Add(new DateTime(2025, 6, 28));
            _holidays.Add(new DateTime(2025, 8, 24));
            _holidays.Add(new DateTime(2025, 10, 14));
            _holidays.Add(new DateTime(2025, 12, 25));
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

            // Підтримка від'ємних значень (віднімання робочих днів)
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

            // Додавання робочих днів (позитивне значення)
            var daysAdded = 0;
            while (daysAdded < workingDays)
            {
                currentDate = GetNextWorkingDay(currentDate);
                daysAdded++;
            }

            return currentDate;
        }

        // Метод для отримання попереднього робочого дня
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
        /// Підраховує кількість робочих днів між двома датами (не включаючи початкову)
        /// </summary>
        public int CountWorkingDaysBetween(DateTime startDate, DateTime endDate)
        {
            if (endDate <= startDate)
                return 0;

            var workingDays = 0;
            var currentDate = startDate.AddDays(1); // Не включаємо початкову дату

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
                return "Субота";
            if (date.DayOfWeek == DayOfWeek.Sunday)
                return "Неділя";
            if (_holidays.Contains(date.Date))
                return GetHolidayName(date.Date);

            return "Робочий день";
        }

        private string GetHolidayName(DateTime date)
        {
            return date switch
            {
                { Month: 1, Day: 1 } => "Новий рік",
                { Month: 1, Day: 7 } => "Різдво",
                { Month: 3, Day: 8 } => "Міжнародний жіночий день",
                { Month: 4, Day: 20 } => "Великдень",
                { Month: 4, Day: 21 } => "Понеділок після Великодня",
                { Month: 6, Day: 8 } => "Трійця",
                { Month: 6, Day: 28 } => "День Конституції",
                { Month: 8, Day: 24 } => "День Незалежності",
                { Month: 10, Day: 14 } => "День захисників і захисниць",
                { Month: 12, Day: 25 } => "Різдво (католицьке)",
                _ => "Святковий день"
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
