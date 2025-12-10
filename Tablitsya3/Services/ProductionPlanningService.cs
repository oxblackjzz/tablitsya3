using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    public class ProductionPlanningService
    {
        private readonly WorkingDaysService _workingDaysService;
        private readonly ILogger<ProductionPlanningService> _logger;

        public ProductionPlanningService(
            WorkingDaysService workingDaysService,
            ILogger<ProductionPlanningService> logger)
        {
            _workingDaysService = workingDaysService;
            _logger = logger;
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

            _logger.LogInformation("[Цех #{WorkshopNumber}] ========== ПОЧАТОК РОЗРАХУНКУ ==========", workshopNumber);
            _logger.LogInformation("[Цех #{WorkshopNumber}] Замовлень: {Count}, Потужність: {Capacity} м²/день", 
                workshopNumber, dailyOrders.Count, dailyCapacity);

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

                // Новий алгоритм планування (v3):
                // 1. Найраніший старт: замовлення + N робочих днів на підготовку
                var earliestPossibleStart = _workingDaysService.AddWorkingDays(orderDate, daysBeforeProduction);
                
                // 2. Шукаємо перший день, коли цех буде мати достатню потужність
                DateTime actualProductionStart = FindNextAvailableStartDate(
                    earliestPossibleStart, 
                    orderSize, 
                    dailyCapacity, 
                    productionDateLoad,
                    workshopNumber);

                if (actualProductionStart > earliestPossibleStart)
                {
                    var delayDays = _workingDaysService.CountWorkingDaysBetween(earliestPossibleStart, actualProductionStart);
                    _logger.LogDebug("[Цех #{WorkshopNumber}] Замовлення #{Day}: Відкладено старт на {DelayDays} робочих днів (з {From:dd.MM.yyyy} до {To:dd.MM.yyyy}) через перевантаження цеху",
                        workshopNumber, day, delayDays, earliestPossibleStart, actualProductionStart);
                }

                _logger.LogDebug("[Цех #{WorkshopNumber}] Замовлення #{Day}: Обсяг={Size} м², Замовлено={OrderDate:dd.MM.yyyy}, Старт={StartDate:dd.MM.yyyy}",
                    workshopNumber, day, orderSize, orderDate, actualProductionStart);

                var remainingOrder = orderSize;
                var currentDate = actualProductionStart;
                var productionStartDate = currentDate;
                DateTime productionEndDate = currentDate;

                var dailyAllocation = new Dictionary<int, double>();
                int dayIndex = 1;
                double totalCapacityOverflow = 0;

                // Якщо є кастомна дата завершення, намагаємо її дотримуватись
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

                            _logger.LogTrace("[Цех #{WorkshopNumber}] День {Date:dd.MM.yyyy}: Виділено {Allocated:F0} м², Залишок {Remaining:F0} м², Завантаження: {Load:F0}/{Capacity}",
                                workshopNumber, currentDate, toAllocate, remainingOrder, productionDateLoad[currentDate], dailyCapacity);

                            dayIndex++;
                        }

                        currentDate = currentDate.AddDays(1);
                    }

                    // Якщо залишилось - додаємо після цільової дати
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

                            dayIndex++;
                        }

                        currentDate = currentDate.AddDays(1);
                    }
                }
                else
                {
                    // Стандартний варіант без кастомної дати
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

                            _logger.LogTrace("[Цех #{WorkshopNumber}] День {Date:dd.MM.yyyy}: Виділено {Allocated:F0} м², Залишок {Remaining:F0} м²",
                                workshopNumber, currentDate, toAllocate, remainingOrder);

                            dayIndex++;
                        }

                        currentDate = currentDate.AddDays(1);
                    }
                }

                var productionDuration = orderSize / dailyCapacity;
                var workingDaysUsed = dailyAllocation.Count;

                // Обчислення ФІНІШУ: +N робочих днів + компенсація за перевищення
                DateTime calculatedFinish = productionEndDate;

                if (!customCompletionDate.HasValue)
                {
                    calculatedFinish = _workingDaysService.AddWorkingDays(productionEndDate, productionLeadTime);

                    if (totalCapacityOverflow > 0)
                    {
                        var additionalDays = (int)Math.Ceiling(totalCapacityOverflow / dailyCapacity);

                        _logger.LogWarning("[Цех #{WorkshopNumber}] Увага: Перевищення потужності на {Overflow:F0} м²! Додаємо {Days} днів",
                            workshopNumber, totalCapacityOverflow, additionalDays);

                        calculatedFinish = calculatedFinish.AddDays(additionalDays);
                    }

                    productionEndDate = calculatedFinish;
                }

                // Відвантаження: наступний робочий день після фінішу
                var completionDate = _workingDaysService.GetNextWorkingDay(productionEndDate);

                var productionStartDayNumber = (int)(productionStartDate - startDate).TotalDays + 1;
                var productionEndDayNumber = (int)(productionEndDate - startDate).TotalDays + 1;

                _logger.LogDebug("[Цех #{WorkshopNumber}] Замовлення #{Day}: Виробництво {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}, Відвантаження {CompletionDate:dd.MM.yyyy}",
                    workshopNumber, day, productionStartDate, productionEndDate, completionDate);

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

            _logger.LogInformation("[Цех #{WorkshopNumber}] ========== КІНЕЦЬ РОЗРАХУНКУ ========== ({Count} замовлень)", 
                workshopNumber, schedule.Orders.Count);

            var dailyLoad = new Dictionary<int, double>();
            foreach (var kvp in productionDateLoad.OrderBy(x => x.Key))
            {
                var dayIdx = (int)(kvp.Key - startDate).TotalDays + 1;
                dailyLoad[dayIdx] = kvp.Value;
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

            // Ітеруємо просто знаходячи, від якого першого дня, коли буде хоча б трохи потужності
            while (remainingToAllocate > 0)
            {
                // Пропускаємо вихідні дні
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
                    // Цей день має потужність - це наш старт!
                    return candidateDate;
                }

                // День повністю завантажений - шукаємо наступний день
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
