using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Tablitsya3.Models;
using Tablitsya3.Services;

namespace Tablitsya3.Components.Pages;

public partial class ProductionPlanning : ComponentBase
{
    [Inject] private ProductionPlanningService PlanningService { get; set; } = default!;
    [Inject] private UnifiedStorageService StorageService { get; set; } = default!;
    [Inject] private WorkshopConfigService WorkshopConfigService { get; set; } = default!;
    [Inject] private WorkingDaysService WorkingDaysService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private LoggingService Logger { get; set; } = default!;

    // Дані
    private WorkshopData workshopData = new();
    
    // Динамічні графіки для всіх цехів (замість окремих schedule1, schedule3, schedule6)
    private Dictionary<int, ProductionSchedule> schedules = new();
    private List<WorkshopConfig> workshops = new();
    
    private bool hasSchedules = false;
    private string statusMessage = string.Empty;
    
    // Ключ для оновлення діаграми Ганта
    private int ganttChartKey = 0;

    // Фільтрація (працює для всіх цехів)
    private string orderFilter = "all";
    private DateTime filterDate = DateTime.Today;

    // Редагування
    private Order? editingOrder;
    private string editOrderName = string.Empty;
    private DateTime editOrderDate = DateTime.Today;
    private DateTime editProductionStart = DateTime.Today;
    private DateTime editProductionEnd = DateTime.Today;
    private DateTime editCompletionDate = DateTime.Today;

    // Властивості для binding дат
    private string EditOrderDateValue
    {
        get => editOrderDate.ToString("yyyy-MM-dd");
        set
        {
            if (DateTime.TryParse(value, out var date))
            {
                editOrderDate = date;
                Logger.LogInfo($"Змінено дату замовлення: {date:dd.MM.yyyy}", "ProductionPlanning");
            }
        }
    }

    private string EditProductionStartValue
    {
        get => editProductionStart.ToString("yyyy-MM-dd");
        set
        {
            if (DateTime.TryParse(value, out var date))
            {
                editProductionStart = date;
                Logger.LogInfo($"Змінено дату початку виробництва: {date:dd.MM.yyyy}", "ProductionPlanning");
            }
        }
    }

    private string EditProductionEndValue
    {
        get => editProductionEnd.ToString("yyyy-MM-dd");
        set
        {
            if (DateTime.TryParse(value, out var date))
            {
                editProductionEnd = date;
                Logger.LogInfo($"Змінено дату кінця виробництва: {date:dd.MM.yyyy}", "ProductionPlanning");
            }
        }
    }

    private string EditCompletionDateValue
    {
        get => editCompletionDate.ToString("yyyy-MM-dd");
        set
        {
            if (DateTime.TryParse(value, out var date))
            {
                editCompletionDate = date;
                Logger.LogInfo($"Змінено дату завершення: {date:dd.MM.yyyy}", "ProductionPlanning");
            }
        }
    }

    // Backward compatibility - отримати графік для конкретного цеху
    private ProductionSchedule? schedule1 => schedules.GetValueOrDefault(1);
    private ProductionSchedule? schedule3 => schedules.GetValueOrDefault(3);
    private ProductionSchedule? schedule6 => schedules.GetValueOrDefault(6);

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private void GoToBulkOrderEntry()
    {
        Navigation.NavigateTo("/bulk-order-entry");
    }

    private async Task ForceRecalculate()
    {
        if (!await JSRuntime.InvokeAsync<bool>("confirm", "Це скине всі ручні зміни дат завершення. Продовжити?"))
            return;

        try
        {
            Logger.LogInfo("Примусовий перерахунок: очищення кастомних дат", "ProductionPlanning");

            var customDatesCount = workshopData.CustomCompletionDates.Count;
            workshopData.CustomCompletionDates.Clear();

            Logger.LogInfo($"Очищено {customDatesCount} кастомних дат", "ProductionPlanning");

            CalculateAllSchedules();
            await SaveAllData();

            statusMessage = $"✅ Графіки перераховано! Очищено {customDatesCount} кастомних дат.";
            Logger.LogInfo("Примусовий перерахунок завершено", "ProductionPlanning");
        }
        catch (Exception ex)
        {
            statusMessage = $"❌ Помилка перерахунку: {ex.Message}";
            Logger.LogError("Помилка примусового перерахунку", ex, "ProductionPlanning");
        }
    }

    private async Task LoadData()
    {
        try
        {
            Logger.LogInfo("Завантаження даних", "ProductionPlanning");

            // Завантажуємо конфігурацію цехів
            workshops = await WorkshopConfigService.GetActiveWorkshopsAsync();
            Logger.LogInfo($"Завантажено {workshops.Count} цехів", "ProductionPlanning");

            var loadedData = await StorageService.LoadWorkshopDataAsync();
            if (loadedData != null)
            {
                workshopData = loadedData;

                // Ініціалізуємо цехи якщо потрібно
                foreach (var workshop in workshops)
                {
                    workshopData.EnsureWorkshopExists(workshop.Number);
                }

                var totalOrders = workshopData.WorkshopOrders.Sum(x => x.Value.Count);
                Logger.LogInfo($"Завантажено {totalOrders} замовлень для {workshopData.WorkshopOrders.Count} цехів", "ProductionPlanning");

                if (workshopData.WorkshopOrders.Any())
                {
                    CalculateAllSchedules();
                }
            }
            else
            {
                Logger.LogWarning("Даних немає, ініціалізація", "ProductionPlanning");
                workshopData.EnsureDefaultWorkshops();
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError("Помилка завантаження даних", ex, "ProductionPlanning");
            statusMessage = $"❌ Помилка завантаження: {ex.Message}";
        }
    }

    /// <summary>
    /// Розрахувати графік для одного цеху (усунення дублювання)
    /// </summary>
    private ProductionSchedule? CalculateScheduleForWorkshop(int workshopNumber)
    {
        if (!workshopData.WorkshopOrders.ContainsKey(workshopNumber) || 
            !workshopData.WorkshopOrders[workshopNumber].Any())
        {
            return null;
        }

        var orders = workshopData.WorkshopOrders[workshopNumber];
        
        var dates = workshopData.WorkshopOrderDates.ContainsKey(workshopNumber)
            ? workshopData.WorkshopOrderDates[workshopNumber]
            : orders.Select((_, i) => workshopData.StartDate.AddDays(i)).ToList();

        var names = workshopData.WorkshopOrderNames.GetValueOrDefault(workshopNumber);

        var capacity = workshopData.GetCapacity(workshopNumber);
        var leadTime = workshopData.GetProductionLeadTime(workshopNumber);
        var daysBefore = workshopData.GetDaysBeforeProduction(workshopNumber);

        var schedule = PlanningService.CalculateSchedule(
            orders,
            workshopData.StartDate,
            capacity,
            leadTime,
            dates,
            names,
            workshopData.CustomCompletionDates,
            workshopNumber,
            daysBefore
        );

        // Встановлюємо номер цеху для всіх замовлень
        foreach (var order in schedule.Orders)
        {
            order.WorkshopNumber = workshopNumber;
        }

        return schedule;
    }

    private void CalculateAllSchedules()
    {
        try
        {
            Logger.LogInfo("Розрахунок графіків для всіх цехів", "ProductionPlanning");
            var startTime = DateTime.Now;

            schedules.Clear();

            // Розраховуємо для всіх активних цехів
            foreach (var workshop in workshops)
            {
                var schedule = CalculateScheduleForWorkshop(workshop.Number);
                if (schedule != null)
                {
                    schedules[workshop.Number] = schedule;
                }
            }

            // Також розраховуємо для цехів з даними, які не в конфігурації
            foreach (var workshopNumber in workshopData.GetAllWorkshopNumbers())
            {
                if (!schedules.ContainsKey(workshopNumber))
                {
                    var schedule = CalculateScheduleForWorkshop(workshopNumber);
                    if (schedule != null)
                    {
                        schedules[workshopNumber] = schedule;
                    }
                }
            }

            hasSchedules = schedules.Values.Any(s => s.Orders.Any());

            if (hasSchedules)
            {
                var elapsed = DateTime.Now - startTime;
                var totalOrders = schedules.Values.Sum(s => s.Orders.Count);
                Logger.LogInfo($"Розраховано графіки: {totalOrders} замовлень за {elapsed.TotalMilliseconds:F0}ms", "ProductionPlanning");

                statusMessage = "✅ Графіки розраховано успішно!";
                ganttChartKey++;
            }
            else
            {
                Logger.LogWarning("Немає замовлень для розрахунку", "ProductionPlanning");
                statusMessage = "⚠️ Немає замовлень для розрахунку.";
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError("Помилка розрахунку графіків", ex, "ProductionPlanning");
            statusMessage = $"❌ Помилка розрахунку: {ex.Message}";
        }
    }

    /// <summary>
    /// Отримати графік для цеху (універсальний метод)
    /// </summary>
    public ProductionSchedule? GetSchedule(int workshopNumber)
    {
        return schedules.GetValueOrDefault(workshopNumber);
    }

    /// <summary>
    /// Отримати всі цехи з графіками
    /// </summary>
    public IEnumerable<(WorkshopConfig Config, ProductionSchedule Schedule)> GetWorkshopsWithSchedules()
    {
        foreach (var workshop in workshops.OrderBy(w => w.SortOrder))
        {
            if (schedules.TryGetValue(workshop.Number, out var schedule) && schedule.Orders.Any())
            {
                yield return (workshop, schedule);
            }
        }
    }

    private async Task SaveAllData()
    {
        try
        {
            Logger.LogInfo("Збереження даних", "ProductionPlanning");

            await StorageService.SaveWorkshopDataAsync(workshopData);

            var totalOrders = workshopData.WorkshopOrders.Sum(x => x.Value.Count);
            Logger.LogInfo($"Дані збережено: {totalOrders} замовлень", "ProductionPlanning");

            statusMessage = "✅ Дані збережено!";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError("Помилка збереження даних", ex, "ProductionPlanning");
            statusMessage = $"❌ Помилка збереження: {ex.Message}";
        }
    }

    private async Task ClearAllData()
    {
        if (!await JSRuntime.InvokeAsync<bool>("confirm", "Видалити всі дані? Цю дію неможливо відмінити."))
            return;

        try
        {
            await StorageService.ClearAllDataAsync();
            workshopData = new WorkshopData();
            schedules.Clear();
            hasSchedules = false;
            statusMessage = "✅ Дані очищено!";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            statusMessage = $"❌ Помилка очищення: {ex.Message}";
        }
    }

    private void StartOrderEdit(int workshopNumber, Order order)
    {
        editingOrder = order;
        editingOrder.WorkshopNumber = workshopNumber;
        editOrderName = order.OrderName ?? string.Empty;
        editOrderDate = order.StartDate;
        editProductionStart = order.ProductionStartDate;
        editProductionEnd = order.ProductionEndDate;
        editCompletionDate = order.CompletionDate;

        Logger.LogInfo($"Редагування замовлення {order.Day} (цех {workshopNumber})", "ProductionPlanning");
        StateHasChanged();
    }

    private bool ValidateOrderEdit()
    {
        return true;
    }

    private async Task SaveOrderEdit(int workshopNumber, Order order)
    {
        try
        {
            Logger.LogInfo($"Збереження змін замовлення {order.Day}, цех {workshopNumber}", "ProductionPlanning");

            if (!ValidateOrderEdit())
            {
                Logger.LogWarning("Валідація не пройдена", "ProductionPlanning");
                return;
            }

            var schedule = GetSchedule(workshopNumber);

            if (schedule == null || !schedule.Orders.Any())
            {
                statusMessage = "❌ Помилка: графік не знайдено";
                Logger.LogError($"Графік не знайдено для цеху {workshopNumber}", null, "ProductionPlanning");
                return;
            }

            var orderToEdit = schedule.Orders.FirstOrDefault(o => o.Day == order.Day);
            if (orderToEdit == null)
            {
                statusMessage = $"❌ Помилка: замовлення {order.Day} не знайдено";
                return;
            }

            var scheduleIndex = schedule.Orders.IndexOf(orderToEdit);

            // Оновлення назви
            if (!string.IsNullOrWhiteSpace(editOrderName))
            {
                orderToEdit.OrderName = editOrderName;

                if (!workshopData.WorkshopOrderNames.ContainsKey(workshopNumber))
                {
                    workshopData.WorkshopOrderNames[workshopNumber] = new List<string>();
                }

                while (workshopData.WorkshopOrderNames[workshopNumber].Count <= scheduleIndex)
                {
                    workshopData.WorkshopOrderNames[workshopNumber].Add(string.Empty);
                }

                workshopData.WorkshopOrderNames[workshopNumber][scheduleIndex] = editOrderName;
            }

            // Оновлення дати замовлення
            if (editOrderDate != order.StartDate)
            {
                orderToEdit.StartDate = editOrderDate;

                if (!workshopData.WorkshopOrderDates.ContainsKey(workshopNumber))
                {
                    workshopData.WorkshopOrderDates[workshopNumber] = new List<DateTime>();
                }

                while (workshopData.WorkshopOrderDates[workshopNumber].Count <= scheduleIndex)
                {
                    workshopData.WorkshopOrderDates[workshopNumber].Add(DateTime.Today);
                }

                workshopData.WorkshopOrderDates[workshopNumber][scheduleIndex] = editOrderDate;
            }

            // Збереження кастомної дати завершення
            var key = $"{workshopNumber}_{order.Day}";
            workshopData.CustomCompletionDates[key] = editCompletionDate;

            editingOrder = null;
            StateHasChanged();

            CalculateAllSchedules();
            await SaveAllData();

            statusMessage = $"✅ Замовлення {order.Day} оновлено!";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            statusMessage = $"❌ Помилка збереження: {ex.Message}";
            Logger.LogError($"Помилка збереження замовлення {order.Day}", ex, "ProductionPlanning");
        }
    }

    private void CancelOrderEdit()
    {
        editingOrder = null;
        StateHasChanged();
    }

    private async Task DeleteOrder(int workshopNumber, int orderDay)
    {
        if (!await JSRuntime.InvokeAsync<bool>("confirm", $"Видалити замовлення {orderDay} цеху {workshopNumber}?"))
            return;

        try
        {
            Logger.LogInfo($"Видалення замовлення {orderDay} цеху {workshopNumber}", "ProductionPlanning");

            var schedule = GetSchedule(workshopNumber);

            if (schedule == null || !schedule.Orders.Any())
            {
                statusMessage = "❌ Помилка: графік не знайдено";
                return;
            }

            var orderToDelete = schedule.Orders.FirstOrDefault(o => o.Day == orderDay);
            if (orderToDelete == null)
            {
                statusMessage = $"❌ Помилка: замовлення {orderDay} не знайдено";
                return;
            }

            var scheduleIndex = schedule.Orders.IndexOf(orderToDelete);

            if (workshopData.WorkshopOrders.ContainsKey(workshopNumber))
            {
                var orders = workshopData.WorkshopOrders[workshopNumber];

                if (scheduleIndex >= 0 && scheduleIndex < orders.Count)
                {
                    orders.RemoveAt(scheduleIndex);

                    if (workshopData.WorkshopOrderDates.ContainsKey(workshopNumber) &&
                        scheduleIndex < workshopData.WorkshopOrderDates[workshopNumber].Count)
                    {
                        workshopData.WorkshopOrderDates[workshopNumber].RemoveAt(scheduleIndex);
                    }

                    if (workshopData.WorkshopOrderNames.ContainsKey(workshopNumber) &&
                        scheduleIndex < workshopData.WorkshopOrderNames[workshopNumber].Count)
                    {
                        workshopData.WorkshopOrderNames[workshopNumber].RemoveAt(scheduleIndex);
                    }

                    var customKey = $"{workshopNumber}_{orderDay}";
                    workshopData.CustomCompletionDates.Remove(customKey);
                }
            }

            CalculateAllSchedules();
            await SaveAllData();

            Logger.LogInfo($"Замовлення {orderDay} видалено з цеху {workshopNumber}", "ProductionPlanning");
            statusMessage = $"✅ Замовлення {orderDay} видалено!";
            StateHasChanged();
        }
        catch (Exception ex)
        {
            statusMessage = $"❌ Помилка видалення: {ex.Message}";
            Logger.LogError($"Помилка видалення замовлення {orderDay}", ex, "ProductionPlanning");
        }
    }

    // Методи фільтрації
    private IEnumerable<Order> FilterOrders(IEnumerable<Order> orders)
    {
        if (orders == null || !orders.Any())
            return Enumerable.Empty<Order>();

        return orderFilter switch
        {
            "in-production" => orders.Where(o => o.IsInProduction(filterDate)).OrderBy(o => o.ProductionStartDate),
            "not-started" => orders.Where(o => o.IsNotStarted(filterDate)).OrderBy(o => o.StartDate),
            "completed" => orders.Where(o => o.IsCompleted(filterDate)).OrderBy(o => o.CompletionDate),
            _ => orders.OrderBy(o => o.StartDate)
        };
    }

    // Методи статистики
    private int GetTotalOrdersCount()
    {
        return schedules.Values.Sum(s => s.Orders.Count);
    }

    private int GetInProductionCount()
    {
        return schedules.Values.Sum(s => s.Orders.Count(o => o.IsInProduction(filterDate)));
    }

    private int GetNotStartedCount()
    {
        return schedules.Values.Sum(s => s.Orders.Count(o => o.IsNotStarted(filterDate)));
    }

    private int GetCompletedCount()
    {
        return schedules.Values.Sum(s => s.Orders.Count(o => o.IsCompleted(filterDate)));
    }

    private string GetFilterName()
    {
        return orderFilter switch
        {
            "in-production" => "В роботі",
            "not-started" => "Очікують",
            "completed" => "Завершено",
            _ => "Усі замовлення"
        };
    }
}
